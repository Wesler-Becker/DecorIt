import * as RNFS from '@dr.pogodin/react-native-fs';
import {Platform} from 'react-native';
import {needsClientCollectionMigration, normalizeClientCollection, seedClients} from './clientDomain';
import type {ClientData} from '../types';

// Unity's Android persistentDataPath is getExternalFilesDir(null); RNFS points to the same directory.
const dataDirectory = Platform.OS === 'android' ? RNFS.ExternalDirectoryPath : RNFS.DocumentDirectoryPath;
const clientsPath = `${dataDirectory}/clients.json`;
const backupPath = `${clientsPath}.bak`;
const temporaryPath = `${clientsPath}.tmp`;

export async function loadClients(): Promise<ClientData[]> {
  for (const path of [clientsPath, backupPath]) {
    try {
      if (!(await RNFS.exists(path))) {
        continue;
      }
      const parsed = JSON.parse(await RNFS.readFile(path, 'utf8'));
      const clients = normalizeClientCollection(parsed).clients;
      if (path === backupPath || needsClientCollectionMigration(parsed)) {
        await writeClientCollection(clients, path === clientsPath);
      }
      return clients;
    } catch {
      // Try the previous copy if a write was interrupted or the file is malformed.
    }
  }

  const initial = seedClients();
  await saveClients(initial);
  return initial;
}

export async function saveClients(clients: ClientData[]): Promise<void> {
  await writeClientCollection(clients, true);
}

async function writeClientCollection(clients: ClientData[], preserveCurrentAsBackup: boolean): Promise<void> {
  await RNFS.mkdir(dataDirectory);
  const json = JSON.stringify({clients: normalizeClientCollection({clients}).clients}, null, 2);
  await RNFS.writeFile(temporaryPath, json, 'utf8');
  if (await RNFS.exists(clientsPath)) {
    if (preserveCurrentAsBackup) {
      await RNFS.copyFile(clientsPath, backupPath);
    }
    await RNFS.unlink(clientsPath);
  }
  await RNFS.moveFile(temporaryPath, clientsPath);
}

export function photoFileUri(path: string): string {
  if (path.startsWith('file://') || path.startsWith('content://')) {
    return path;
  }
  return `file://${path}`;
}

export const legacyClientsFilePath = clientsPath;
