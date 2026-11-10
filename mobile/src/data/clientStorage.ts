import * as RNFS from '@dr.pogodin/react-native-fs';
import {Platform} from 'react-native';
import {open, type Transaction} from '@op-engineering/op-sqlite';
import {drizzle, type OPSQLiteDatabase} from 'drizzle-orm/op-sqlite';
import migrations from '../../drizzle/migrations';
import {createId, normalizeClientCollection, seedClients} from './clientDomain';
import {
  appMetaTable,
  clientsTable,
  photosTable,
  recordsTable,
  roomsTable,
  schema,
} from './schema';
import type {ClientData, PhotoData, RoomData, VisitDraft, VisitRecordData} from '../types';

// This folder is shared with Unity for the JSON import and AR photo outbox.
const sharedDataDirectory = Platform.OS === 'android' ? RNFS.ExternalDirectoryPath : RNFS.DocumentDirectoryPath;
const legacyClientsPath = `${sharedDataDirectory}/clients.json`;
const legacyBackupPath = `${legacyClientsPath}.bak`;
const lastARConfigPath = `${sharedDataDirectory}/last_ar_config.json`;
const photoOutboxDirectory = `${sharedDataDirectory}/DecorIt/PhotoOutbox`;
const legacyImportKey = 'migration.clients_json.v1';

type AppDatabase = OPSQLiteDatabase<typeof schema> & {$client: ReturnType<typeof open>};
let databasePromise: Promise<AppDatabase> | null = null;
let photoOutboxImportPromise: Promise<void> | null = null;
let legacyImportPromise: Promise<void> | null = null;
let storageTaskQueue: Promise<void> = Promise.resolve();

type UnityPhotoEvent = {
  clientId: string;
  roomId?: string;
  recordId?: string;
  path: string;
  roomName?: string;
  capturedAt?: string;
};

function getDatabase(): Promise<AppDatabase> {
  if (!databasePromise) {
    databasePromise = (async () => {
      const connection = open({name: 'decorit.db'});
      connection.executeSync('PRAGMA foreign_keys = ON');
      await applyGeneratedMigrations(connection);
      const db = drizzle(connection, {schema}) as AppDatabase;
      return db;
    })().catch(error => {
      console.error('[DecorIt][storage] Falha ao abrir SQLite ou aplicar migrations.', error);
      databasePromise = null;
      throw error;
    });
  }
  return databasePromise;
}

async function applyGeneratedMigrations(connection: ReturnType<typeof open>): Promise<void> {
  connection.executeSync(`CREATE TABLE IF NOT EXISTS "__drizzle_migrations" (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    hash TEXT NOT NULL,
    created_at NUMERIC
  )`);
  const lastMigration = connection.executeSync(
    'SELECT created_at FROM "__drizzle_migrations" ORDER BY created_at DESC LIMIT 1',
  ).rows[0];
  let latestAppliedAt = lastMigration ? Number(lastMigration.created_at) : -1;

  for (const entry of migrations.journal.entries) {
    if (entry.when <= latestAppliedAt) continue;
    const sql = migrations.migrations[`m${entry.idx.toString().padStart(4, '0')}`];
    if (!sql) throw new Error(`Migration ausente: ${entry.tag}`);
    const statements = sql.split('--> statement-breakpoint').map(statement => statement.trim()).filter(Boolean);
    await connection.transaction(async tx => {
      for (const statement of statements) await tx.execute(statement);
      await tx.execute('INSERT INTO "__drizzle_migrations" (hash, created_at) VALUES (?, ?)', ['', entry.when]);
    });
    latestAppliedAt = entry.when;
  }
}

export async function loadLastARConfig(): Promise<Partial<VisitDraft>> {
  try {
    if (!(await RNFS.exists(lastARConfigPath))) return {};
    return JSON.parse(await RNFS.readFile(lastARConfigPath, 'utf8')) as Partial<VisitDraft>;
  } catch (error) {
    console.warn('[DecorIt][storage] Não foi possível ler a última configuração AR; usando os padrões.', error);
  }
  return {};
}

export async function loadClients(): Promise<ClientData[]> {
  return serializeStorageTask(async () => {
    const db = await getDatabase();
    await importLegacyCollectionOnce(db);
    await importUnityPhotoOutbox(db);
    return readClientCollection(db);
  });
}

export async function saveClients(clients: ClientData[]): Promise<void> {
  return serializeStorageTask(async () => {
    const db = await getDatabase();
    await db.$client.transaction(tx => replaceClientCollection(tx, clients, db));
  });
}

function serializeStorageTask<T>(task: () => Promise<T>): Promise<T> {
  const result = storageTaskQueue.then(task);
  storageTaskQueue = result.then(() => undefined, () => undefined);
  return result;
}

function importLegacyCollectionOnce(db: AppDatabase): Promise<void> {
  if (!legacyImportPromise) {
    legacyImportPromise = importLegacyCollection(db).finally(() => {
      legacyImportPromise = null;
    });
  }
  return legacyImportPromise;
}

async function importLegacyCollection(db: AppDatabase): Promise<void> {
  const markerResult = await db.$client.execute('SELECT key FROM app_meta WHERE key = ? LIMIT 1', [legacyImportKey]);
  const marker = markerResult.rows[0];
  if (marker) return;

  let legacyCollection: unknown = null;
  let legacyFileFound = false;
  let legacyFileParsed = false;
  for (const path of [legacyClientsPath, legacyBackupPath]) {
    try {
      if (await RNFS.exists(path)) {
        legacyFileFound = true;
        legacyCollection = JSON.parse(await RNFS.readFile(path, 'utf8'));
        legacyFileParsed = true;
        break;
      }
    } catch {
      // If clients.json was interrupted, try the previous atomic-write backup.
      console.warn(`[DecorIt][storage] Não foi possível ler ${path}; tentando o próximo backup.`);
    }
  }

  if (legacyFileFound && !legacyFileParsed)
    throw new Error('Não foi possível ler clients.json nem seu backup; os dados antigos foram preservados para recuperação.');

  const clients = !legacyFileParsed
    ? seedClients()
    : normalizeClientCollection(legacyCollection).clients;

  await db.$client.transaction(async tx => {
    const existing = await tx.execute('SELECT id FROM clients LIMIT 1');
    if (existing.rows.length === 0 && clients.length) await replaceClientCollection(tx, clients, db);
    await tx.execute('INSERT OR IGNORE INTO app_meta (key, value) VALUES (?, ?)', [legacyImportKey, 'done']);
  });
}

function importUnityPhotoOutbox(db: AppDatabase): Promise<void> {
  if (!photoOutboxImportPromise) {
    photoOutboxImportPromise = importUnityPhotoOutboxFiles(db).finally(() => {
      photoOutboxImportPromise = null;
    });
  }
  return photoOutboxImportPromise;
}

async function importUnityPhotoOutboxFiles(db: AppDatabase): Promise<void> {
  if (!(await RNFS.exists(photoOutboxDirectory))) return;
  const files = await RNFS.readDir(photoOutboxDirectory);
  for (const file of files.filter(item => item.isFile() && item.name.endsWith('.json'))) {
    try {
      const photo = JSON.parse(await RNFS.readFile(file.path, 'utf8')) as UnityPhotoEvent;
      if (!photo.path || !photo.clientId) continue;

      await db.$client.transaction(async tx => {
        const client = await tx.execute('SELECT id FROM clients WHERE id = ? LIMIT 1', [photo.clientId]);
        if (client.rows.length === 0) throw new Error(`Cliente ${photo.clientId} não encontrado ao importar foto da Unity.`);
        const roomId = photo.roomId || null;
        const recordId = photo.recordId || null;
        let roomName = photo.roomName || 'Não informado';
        if (roomId) {
          const room = await tx.execute('SELECT id, name FROM rooms WHERE id = ? AND client_id = ? LIMIT 1', [roomId, photo.clientId]);
          if (room.rows.length === 0) throw new Error(`Cômodo ${roomId} não encontrado ao importar foto da Unity.`);
          roomName = String(room.rows[0].name || '');
        }
        if (recordId) {
          const record = await tx.execute('SELECT id FROM visit_records WHERE id = ? AND room_id = ? LIMIT 1', [recordId, roomId]);
          if (!roomId || record.rows.length === 0)
            throw new Error(`Registro ${recordId} não encontrado ao importar foto da Unity.`);
        }

        const inserted = await tx.execute(
          'INSERT OR IGNORE INTO photos (id, client_id, room_id, record_id, path, room_name, captured_at, sort_order) VALUES (?, ?, ?, ?, ?, ?, ?, ?)',
          [createId(), photo.clientId, roomId, recordId, photo.path, roomName, photo.capturedAt || null, Date.now()],
        );

        if (roomId && inserted.rowsAffected > 0) {
          const photoCount = await tx.execute('SELECT COUNT(*) AS count FROM photos WHERE room_id = ?', [roomId]);
          const count = Number(photoCount.rows[0]?.count || 0);
          await tx.execute('UPDATE rooms SET image_label = ? WHERE id = ?', [count === 1 ? '1 foto' : `${count} fotos`, roomId]);
        }
      });

      // The unique photo path makes retrying safe if the process stops before this unlink.
      await RNFS.unlink(file.path);
    } catch (error) {
      console.warn('Foto da fila Unity preservada para nova tentativa:', error);
    }
  }
}

async function replaceClientCollection(tx: Transaction, source: ClientData[], db: OPSQLiteDatabase<typeof schema>): Promise<void> {
  const normalized = normalizeClientCollection({clients: source}).clients;
  await runDrizzleStatement(tx, db.delete(clientsTable));

  for (let clientIndex = 0; clientIndex < normalized.length; clientIndex++) {
    const client = normalized[clientIndex];
    await runDrizzleStatement(tx, db.insert(clientsTable).values({
      id: client.id, name: client.name, phone: client.phone, address: client.address,
      postalCode: client.postalCode, street: client.street, complement: client.complement,
      neighborhood: client.neighborhood, city: client.city, state: client.state,
      observations: client.observations, sortOrder: clientIndex,
    }));

    for (let roomIndex = 0; roomIndex < client.rooms.length; roomIndex++) {
      const room = client.rooms[roomIndex];
      await runDrizzleStatement(tx, db.insert(roomsTable).values({
        id: room.id, clientId: client.id, name: room.name,
        imageLabel: room.imageLabel, sortOrder: roomIndex,
      }));

      for (let recordIndex = 0; recordIndex < room.records.length; recordIndex++) {
        const record = room.records[recordIndex];
        await runDrizzleStatement(tx, db.insert(recordsTable).values({
          id: record.id, roomId: room.id, createdAt: record.createdAt, notes: record.notes,
          roomNotes: record.roomNotes || '', openingName: record.openingName,
          widthCentimeters: record.widthCentimeters, heightCentimeters: record.heightCentimeters,
          productCategory: record.productCategory, modelName: record.modelName,
          panelType: record.panelType, fabricMaterialName: record.fabricMaterialName,
          rodMaterialName: record.rodMaterialName, sortOrder: recordIndex,
        }));
        await insertPhotos(tx, db, client.id, room.id, record.id, record.photos, recordIndex);
      }
      await insertPhotos(tx, db, client.id, room.id, null, room.photos, roomIndex);
    }
    await insertPhotos(tx, db, client.id, null, null, client.unassignedPhotos, 0);
  }
}

async function runDrizzleStatement(tx: Transaction, query: {toSQL: () => {sql: string; params: unknown[]}}): Promise<void> {
  const statement = query.toSQL();
  await tx.execute(statement.sql, statement.params as (string | number | boolean | null | ArrayBuffer | ArrayBufferView)[]);
}

async function insertPhotos(
  tx: Transaction,
  db: OPSQLiteDatabase<typeof schema>,
  clientId: string,
  roomId: string | null,
  recordId: string | null,
  photos: PhotoData[],
  baseOrder: number,
): Promise<void> {
  for (let index = 0; index < photos.length; index++) {
    const photo = photos[index];
    if (!photo.path) continue;
    await runDrizzleStatement(tx, db.insert(photosTable).values({
      id: createId(), clientId, roomId, recordId, path: photo.path,
      roomName: photo.roomName || '', capturedAt: photo.capturedAt || null,
      sortOrder: baseOrder * 10000 + index,
    }).onConflictDoNothing());
  }
}

type ClientSqlRow = Pick<ClientData,
  'id' | 'name' | 'phone' | 'address' | 'postalCode' | 'street' | 'complement' |
  'neighborhood' | 'city' | 'state' | 'observations'>;
type RoomSqlRow = Pick<RoomData, 'id' | 'name' | 'imageLabel'> & {clientId: string};
type RecordSqlRow = Pick<VisitRecordData,
  'id' | 'createdAt' | 'notes' | 'roomNotes' | 'openingName' | 'widthCentimeters' |
  'heightCentimeters' | 'productCategory' | 'modelName' | 'panelType' |
  'fabricMaterialName' | 'rodMaterialName'> & {roomId: string};
type PhotoSqlRow = Pick<PhotoData, 'path' | 'roomName' | 'capturedAt'> & {
  clientId: string;
  roomId: string | null;
  recordId: string | null;
};

async function readClientCollection(db: AppDatabase): Promise<ClientData[]> {
  // OP-SQLite's async execute API is reliable here; Drizzle's SELECT adapter
  // throws on-device with the current OP-SQLite/Drizzle combination.
  const [clientResult, roomResult, recordResult, photoResult] = await Promise.all([
    db.$client.execute('SELECT id, name, phone, address, postal_code AS postalCode, street, complement, neighborhood, city, state, observations, sort_order AS sortOrder FROM clients ORDER BY sort_order ASC'),
    db.$client.execute('SELECT id, client_id AS clientId, name, image_label AS imageLabel, sort_order AS sortOrder FROM rooms ORDER BY sort_order ASC'),
    db.$client.execute('SELECT id, room_id AS roomId, created_at AS createdAt, notes, room_notes AS roomNotes, opening_name AS openingName, width_centimeters AS widthCentimeters, height_centimeters AS heightCentimeters, product_category AS productCategory, model_name AS modelName, panel_type AS panelType, fabric_material_name AS fabricMaterialName, rod_material_name AS rodMaterialName, sort_order AS sortOrder FROM visit_records ORDER BY sort_order ASC'),
    db.$client.execute('SELECT id, client_id AS clientId, room_id AS roomId, record_id AS recordId, path, room_name AS roomName, captured_at AS capturedAt, sort_order AS sortOrder FROM photos ORDER BY sort_order ASC'),
  ]);
  const clientRows = clientResult.rows as unknown as ClientSqlRow[];
  const roomRows = roomResult.rows as unknown as RoomSqlRow[];
  const recordRows = recordResult.rows as unknown as RecordSqlRow[];
  const photoRows = photoResult.rows as unknown as PhotoSqlRow[];

  const clients: ClientData[] = clientRows.map(client => ({
    id: client.id, name: client.name, phone: client.phone, address: client.address,
    postalCode: client.postalCode, street: client.street, complement: client.complement,
    neighborhood: client.neighborhood, city: client.city, state: client.state,
    observations: client.observations, rooms: [], unassignedPhotos: [],
  }));
  const clientById = new Map(clients.map(client => [client.id, client]));
  const roomById = new Map<string, RoomData>();
  const recordById = new Map<string, VisitRecordData>();

  for (const room of roomRows) {
    const owner = clientById.get(room.clientId);
    if (!owner) continue;
    const value: RoomData = {id: room.id, name: room.name, imageLabel: room.imageLabel, records: [], photos: []};
    owner.rooms.push(value);
    roomById.set(value.id, value);
  }
  for (const record of recordRows) {
    const owner = roomById.get(record.roomId);
    if (!owner) continue;
    const value: VisitRecordData = {
      id: record.id, createdAt: record.createdAt, notes: record.notes,
      ...(record.roomNotes ? {roomNotes: record.roomNotes} : {}),
      openingName: record.openingName, widthCentimeters: record.widthCentimeters,
      heightCentimeters: record.heightCentimeters, productCategory: record.productCategory,
      modelName: record.modelName, panelType: record.panelType,
      fabricMaterialName: record.fabricMaterialName, rodMaterialName: record.rodMaterialName,
      photos: [],
    };
    owner.records.push(value);
    recordById.set(value.id, value);
  }
  for (const photo of photoRows) {
    const value: PhotoData = {
      path: photo.path, roomName: photo.roomName, recordId: photo.recordId || '',
      ...(photo.capturedAt ? {capturedAt: photo.capturedAt} : {}),
    };
    if (photo.recordId) recordById.get(photo.recordId)?.photos.push(value);
    else if (photo.roomId) roomById.get(photo.roomId)?.photos.push(value);
    else clientById.get(photo.clientId)?.unassignedPhotos.push(value);
  }
  return clients;
}

export function photoFileUri(path: string): string {
  if (path.startsWith('file://') || path.startsWith('content://')) return path;
  return `file://${path}`;
}

export const legacyClientsFilePath = legacyClientsPath;
