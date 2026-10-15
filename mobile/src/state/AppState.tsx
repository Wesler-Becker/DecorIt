import React, {createContext, useCallback, useContext, useEffect, useMemo, useRef, useState} from 'react';
import {assignClientPhotoToRoom as moveClientPhotoToRoom, createId, createVisitRecord, validateRoomName} from '../data/clientDomain';
import {loadClients, saveClients} from '../data/clientStorage';
import type {ClientData, ClientDetails, RoomData, VisitDraft, VisitRecordData} from '../types';

type AppStateValue = {
  clients: ClientData[];
  ready: boolean;
  reload: () => Promise<void>;
  addClient: (name: string, details?: Partial<ClientDetails>) => Promise<ClientData>;
  updateClient: (clientId: string, patch: Pick<ClientData, 'name'> & ClientDetails) => Promise<void>;
  addRoom: (clientId: string, name: string, pendingPhotoPath?: string) => Promise<RoomData>;
  addRecord: (clientId: string, roomId: string, draft: VisitDraft) => Promise<VisitRecordData>;
  updateRecord: (clientId: string, roomId: string, recordId: string, patch: Partial<Pick<VisitRecordData, 'openingName' | 'notes'>>) => Promise<void>;
  assignUnassignedPhoto: (clientId: string, path: string, roomId: string) => Promise<void>;
};

const AppStateContext = createContext<AppStateValue | null>(null);

export function AppStateProvider({children}: React.PropsWithChildren) {
  const [clients, setClients] = useState<ClientData[]>([]);
  const [ready, setReady] = useState(false);
  const queue = useRef<Promise<unknown>>(Promise.resolve());

  const reload = useCallback(async () => {
    const loaded = await loadClients();
    setClients(loaded);
    setReady(true);
  }, []);

  useEffect(() => { void reload(); }, [reload]);

  const mutate = useCallback(<T,>(operation: (current: ClientData[]) => {clients: ClientData[]; result: T}): Promise<T> => {
    const next = queue.current.then(async () => {
      const current = await loadClients();
      const outcome = operation(current);
      await saveClients(outcome.clients);
      setClients(outcome.clients);
      return outcome.result;
    });
    queue.current = next.catch(() => undefined);
    return next;
  }, []);

  const addClient = useCallback((name: string, details: Partial<ClientDetails> = {}) => mutate(current => {
    const client: ClientData = {
      id: createId(), name: name.trim(), phone: details.phone?.trim() || '', address: details.address?.trim() || '',
      postalCode: details.postalCode?.trim() || '', street: details.street?.trim() || '', complement: details.complement?.trim() || '',
      neighborhood: details.neighborhood?.trim() || '', city: details.city?.trim() || '', state: details.state?.trim() || '',
      observations: details.observations?.trim() || '', rooms: [], unassignedPhotos: [],
    };
    return {clients: [...current, client], result: client};
  }), [mutate]);

  const updateClient = useCallback((clientId: string, patch: Pick<ClientData, 'name'> & ClientDetails) => mutate(current => ({
    clients: current.map(client => client.id === clientId ? {
      ...client, ...patch,
      name: patch.name.trim(), phone: patch.phone.trim(), address: patch.address.trim(), postalCode: patch.postalCode.trim(),
      street: patch.street.trim(), complement: patch.complement.trim(), neighborhood: patch.neighborhood.trim(),
      city: patch.city.trim(), state: patch.state.trim(), observations: patch.observations.trim(),
    } : client),
    result: undefined,
  })), [mutate]);

  const addRoom = useCallback((clientId: string, name: string, pendingPhotoPath?: string) => mutate(current => {
    const client = current.find(item => item.id === clientId);
    if (!client) throw new Error('Cliente não encontrado.');
    const validationError = validateRoomName(name, client.rooms);
    if (validationError) throw new Error(validationError);
    const room: RoomData = {id: createId(), name: name.trim().replace(/\s+/g, ' '), imageLabel: 'Sem fotos', records: [], photos: []};
    const withRoom = current.map(item => item.id === clientId ? {...item, rooms: [...item.rooms, room]} : item);
    const updatedClients = pendingPhotoPath ? moveClientPhotoToRoom(withRoom, clientId, pendingPhotoPath, room.id) : withRoom;
    return {clients: updatedClients, result: room};
  }), [mutate]);

  const addRecord = useCallback((clientId: string, roomId: string, draft: VisitDraft) => mutate(current => {
    const record = createVisitRecord(draft);
    const found = current.some(client => client.id === clientId && client.rooms.some(room => room.id === roomId));
    if (!found) throw new Error('Cômodo não encontrado para este cliente.');
    return {
      clients: current.map(client => client.id !== clientId ? client : {
        ...client,
        rooms: client.rooms.map(room => room.id === roomId ? {...room, records: [...room.records, record]} : room),
      }),
      result: record,
    };
  }), [mutate]);

  const updateRecord = useCallback((clientId: string, roomId: string, recordId: string, patch: Partial<Pick<VisitRecordData, 'openingName' | 'notes'>>) => mutate(current => {
    const found = current.some(client => client.id === clientId && client.rooms.some(room => room.id === roomId && room.records.some(record => record.id === recordId)));
    if (!found) throw new Error('Registro não encontrado para este cômodo.');
    return {
      clients: current.map(client => client.id !== clientId ? client : {
        ...client,
        rooms: client.rooms.map(room => room.id !== roomId ? room : {
          ...room,
          records: room.records.map(record => record.id === recordId ? {
            ...record,
            ...(patch.openingName !== undefined ? {openingName: patch.openingName.trim()} : {}),
            ...(patch.notes !== undefined ? {notes: patch.notes.trim()} : {}),
          } : record),
        }),
      }),
      result: undefined,
    };
  }), [mutate]);

  const assignUnassignedPhoto = useCallback((clientId: string, path: string, roomId: string) => mutate(current => {
    return {clients: moveClientPhotoToRoom(current, clientId, path, roomId), result: undefined};
  }), [mutate]);

  const value = useMemo(() => ({clients, ready, reload, addClient, updateClient, addRoom, addRecord, updateRecord, assignUnassignedPhoto}),
    [clients, ready, reload, addClient, updateClient, addRoom, addRecord, updateRecord, assignUnassignedPhoto]);

  return <AppStateContext.Provider value={value}>{children}</AppStateContext.Provider>;
}

export function useAppState(): AppStateValue {
  const value = useContext(AppStateContext);
  if (!value) throw new Error('useAppState precisa estar dentro de AppStateProvider.');
  return value;
}

export type CatalogChoice = Pick<VisitDraft, 'productCategory' | 'modelName' | 'panelType' | 'fabricMaterialName' | 'fabricMaterialPath' | 'rodMaterialName' | 'rodMaterialPath'>;
const defaultCatalogChoice: CatalogChoice = {
  productCategory: 'Cortinas', modelName: 'CurtainPrefab', panelType: 'DoublePanel',
  fabricMaterialName: 'CurtainFabric', fabricMaterialPath: 'Assets/Materials/CurtainFabric.mat',
  rodMaterialName: 'RodMaterial', rodMaterialPath: 'Assets/Materials/RodMaterial.mat',
};
type DraftContextValue = {
  draft: VisitDraft | null;
  setDraft: React.Dispatch<React.SetStateAction<VisitDraft | null>>;
  catalogChoice: CatalogChoice;
  setCatalogChoice: React.Dispatch<React.SetStateAction<CatalogChoice>>;
};
const DraftContext = createContext<DraftContextValue | null>(null);

export function DraftProvider({children}: React.PropsWithChildren) {
  const [draft, setDraft] = useState<VisitDraft | null>(null);
  const [catalogChoice, setCatalogChoice] = useState(defaultCatalogChoice);
  return <DraftContext.Provider value={{draft, setDraft, catalogChoice, setCatalogChoice}}>{children}</DraftContext.Provider>;
}

export function useVisitDraft(): DraftContextValue {
  const value = useContext(DraftContext);
  if (!value) throw new Error('useVisitDraft precisa estar dentro de DraftProvider.');
  return value;
}
