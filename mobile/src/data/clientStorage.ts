import * as RNFS from '@dr.pogodin/react-native-fs';
import {NativeModules, Platform} from 'react-native';
import {open, type Transaction} from '@op-engineering/op-sqlite';
import {drizzle, type OPSQLiteDatabase} from 'drizzle-orm/op-sqlite';
import migrations from '../../drizzle/migrations';
import {createId, normalizeClientCollection} from './clientDomain';
import {
  clientsTable,
  photosTable,
  recordsTable,
  roomsTable,
  schema,
} from './schema';
import type {ClientData, PhotoData, RoomData, VisitDraft, VisitRecordData} from '../types';

type AppDatabase = OPSQLiteDatabase<typeof schema> & {$client: ReturnType<typeof open>};
let databasePromise: Promise<AppDatabase> | null = null;
let storageTaskQueue: Promise<void> = Promise.resolve();

async function removeOldJsonFiles(): Promise<void> {
  const base = Platform.OS === 'android' ? RNFS.ExternalDirectoryPath : RNFS.DocumentDirectoryPath;
  if (!base) return;
  for (const name of ['clients.json', 'clients.json.bak', 'last_ar_config.json']) {
    const path = `${base}/${name}`;
    if (await RNFS.exists(path)) await RNFS.unlink(path);
  }
  const outbox = `${base}/DecorIt/PhotoOutbox`;
  if (await RNFS.exists(outbox)) {
    for (const item of await RNFS.readDir(outbox))
      if (item.isFile() && (item.name.endsWith('.json') || item.name.endsWith('.json.tmp'))) await RNFS.unlink(item.path);
  }
}

const clientSecretFields = ['name', 'phone', 'address', 'postalCode', 'street', 'complement', 'neighborhood', 'city', 'state', 'observations'] as const;
const recordSecretFields = ['notes', 'roomNotes', 'openingName'] as const;

async function transformFields<T extends object>(rows: T[], fields: readonly string[], encrypt: boolean): Promise<T[]> {
  if (Platform.OS !== 'android' || !NativeModules.SecureFields?.transform)
    throw new Error('Criptografia segura indisponível neste dispositivo.');
  const copies = rows.map(row => ({...row})) as Array<Record<string, unknown>>;
  const values = copies.flatMap(row => fields.map(field => String(row[field] || '')));
  if (!values.length) return copies as T[];
  const result: string[] = await NativeModules.SecureFields.transform(values, encrypt);
  if (result.length !== values.length) throw new Error('Falha ao processar campos protegidos.');
  let index = 0;
  for (const row of copies) for (const field of fields) row[field] = result[index++];
  return copies as T[];
}

async function protectExistingData(connection: ReturnType<typeof open>): Promise<void> {
  const marker = connection.executeSync("SELECT value FROM app_meta WHERE key = 'secure_fields_v1'").rows[0];
  if (marker) return;
  const clientRows = connection.executeSync('SELECT id, name, phone, address, postal_code AS postalCode, street, complement, neighborhood, city, state, observations FROM clients').rows as unknown as ClientSqlRow[];
  const recordRows = connection.executeSync('SELECT id, notes, room_notes AS roomNotes, opening_name AS openingName FROM visit_records').rows as unknown as RecordSqlRow[];
  const clients = await transformFields(clientRows, clientSecretFields, true);
  const records = await transformFields(recordRows, recordSecretFields, true);
  await connection.transaction(async tx => {
    for (const c of clients) await tx.execute('UPDATE clients SET name=?, phone=?, address=?, postal_code=?, street=?, complement=?, neighborhood=?, city=?, state=?, observations=? WHERE id=?',
      [c.name, c.phone, c.address, c.postalCode, c.street, c.complement, c.neighborhood, c.city, c.state, c.observations, c.id]);
    for (const r of records) await tx.execute('UPDATE visit_records SET notes=?, room_notes=?, opening_name=? WHERE id=?', [r.notes, r.roomNotes || '', r.openingName, r.id]);
    await tx.execute("INSERT INTO app_meta (key, value) VALUES ('secure_fields_v1', 'done')");
  });
}

function getDatabase(): Promise<AppDatabase> {
  if (!databasePromise) {
    databasePromise = (async () => {
      const connection = open({name: 'decorit.db'});
      try {
        connection.executeSync('PRAGMA foreign_keys = ON');
        connection.executeSync('PRAGMA secure_delete = ON');
        await applyGeneratedMigrations(connection);
        await protectExistingData(connection);
        if (!connection.executeSync("SELECT value FROM app_meta WHERE key = 'secure_cleanup_v1'").rows.length) {
          connection.executeSync('PRAGMA wal_checkpoint(TRUNCATE)');
          connection.executeSync('VACUUM');
          connection.executeSync('PRAGMA wal_checkpoint(TRUNCATE)');
          connection.executeSync("INSERT INTO app_meta (key, value) VALUES ('secure_cleanup_v1', 'done')");
        }
        await removeOldJsonFiles();
        return drizzle(connection, {schema}) as AppDatabase;
      } catch (error) {
        connection.close();
        throw error;
      }
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
    await getDatabase();
    const value: string = await NativeModules.UnityArModule.loadCurtainSettings();
    if (!value) return {};
    const settings = JSON.parse(value) as NonNullable<VisitDraft['curtainSettings']> & {widthCentimeters: number; heightCentimeters: number; panelType: VisitDraft['panelType']; fabricMaterialName: string; fabricMaterialPath: string};
    return {curtainSettings: settings, modelName: settings.modelName, widthCentimeters: settings.widthCentimeters, heightCentimeters: settings.heightCentimeters, panelType: settings.panelType, fabricMaterialName: settings.fabricMaterialName, fabricMaterialPath: settings.fabricMaterialPath, rodMaterialName: settings.rodMaterialName, rodMaterialPath: settings.rodMaterialPath};
  } catch (error) {
    console.warn('[DecorIt][storage] Não foi possível ler a última configuração AR; usando os padrões.', error);
  }
  return {};
}

export async function loadClients(): Promise<ClientData[]> {
  return serializeStorageTask(async () => {
    const db = await getDatabase();
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

async function replaceClientCollection(tx: Transaction, source: ClientData[], db: OPSQLiteDatabase<typeof schema>): Promise<void> {
  const normalized = normalizeClientCollection({clients: source}).clients;
  const protectedClients = await transformFields(normalized, clientSecretFields, true);
  for (const client of protectedClients)
    for (const room of client.rooms)
      room.records = await transformFields(room.records, recordSecretFields, true);
  await runDrizzleStatement(tx, db.delete(clientsTable));

  for (let clientIndex = 0; clientIndex < protectedClients.length; clientIndex++) {
    const client = protectedClients[clientIndex];
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
  const clientRows = await transformFields(clientResult.rows as unknown as ClientSqlRow[], clientSecretFields, false);
  const roomRows = roomResult.rows as unknown as RoomSqlRow[];
  const recordRows = await transformFields(recordResult.rows as unknown as RecordSqlRow[], recordSecretFields, false);
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
