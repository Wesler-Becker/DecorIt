import type {ClientData, PhotoData, PhotoItem, RoomData, VisitDraft, VisitRecordData} from '../types';

export type ClientCollection = {clients: ClientData[]};
export const UNASSIGNED_ROOM_NAME = 'Não informado';

export function isUnassignedRoom(room: RoomData): boolean {
  return room.name.trim().toLocaleLowerCase('pt-BR') === UNASSIGNED_ROOM_NAME.toLocaleLowerCase('pt-BR');
}

function normalizeRoomName(name: string): string {
  return name.trim().replace(/\s+/g, ' ').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase('pt-BR');
}

export function validateRoomName(name: string, rooms: RoomData[]): string | null {
  const normalizedName = normalizeRoomName(name);
  if (!normalizedName) return 'Informe o nome do cômodo.';
  if (normalizedName === normalizeRoomName(UNASSIGNED_ROOM_NAME)) return '“Não informado” é um nome reservado para fotos sem cômodo.';
  if (rooms.some(room => normalizeRoomName(room.name) === normalizedName)) return 'Este cliente já possui um cômodo com esse nome.';
  return null;
}

export function createId(): string {
  return `${Date.now().toString(36)}${Math.random().toString(36).slice(2, 12)}`;
}

export function normalizeClientCollection(value: unknown): ClientCollection {
  const raw = value as {clients?: unknown} | null;
  const entries = Array.isArray(raw?.clients) ? raw.clients : [];
  const clients: ClientData[] = entries.filter(Boolean).map((entry: any) => ({
    id: String(entry.id || createId()),
    name: String(entry.name || ''),
    phone: String(entry.phone || ''),
    address: String(entry.address || [entry.street, entry.complement, entry.neighborhood,
      [entry.city, entry.state].filter(Boolean).join(' - '), entry.postalCode].filter(Boolean).join(', ')),
    postalCode: String(entry.postalCode || ''),
    street: String(entry.street || ''),
    complement: String(entry.complement || ''),
    neighborhood: String(entry.neighborhood || ''),
    city: String(entry.city || ''),
    state: String(entry.state || ''),
    observations: String(entry.observations || ''),
    unassignedPhotos: normalizePhotos(entry.unassignedPhotos, '', ''),
    rooms: (Array.isArray(entry.rooms) ? entry.rooms : []).filter(Boolean).map((room: any): RoomData => ({
      id: String(room.id || createId()),
      name: String(room.name || ''),
      imageLabel: String(room.imageLabel || 'Sem fotos'),
      photos: normalizePhotos(room.photos, room.name, ''),
      records: (Array.isArray(room.records) ? room.records : []).filter(Boolean).map((record: any) => ({
        id: String(record.id || createId()),
        createdAt: String(record.createdAt || ''),
        notes: String(record.notes || ''),
        ...(record.roomNotes !== undefined ? {roomNotes: String(record.roomNotes || '')} : {}),
        openingName: String(record.openingName || ''),
        widthCentimeters: finiteNumber(record.widthCentimeters),
        heightCentimeters: finiteNumber(record.heightCentimeters),
        productCategory: String(record.productCategory || 'Cortinas'),
        modelName: String(record.modelName || 'Cortina sob medida'),
        panelType: String(record.panelType || 'DoublePanel'),
        fabricMaterialName: String(record.fabricMaterialName || 'CurtainFabric'),
        rodMaterialName: String(record.rodMaterialName || 'RodMaterial'),
        photos: normalizePhotos(record.photos, room.name, record.id),
      })),
    })),
  }));
  return {clients};
}

export function needsClientCollectionMigration(value: unknown): boolean {
  const raw = value as {clients?: unknown} | null;
  if (!Array.isArray(raw?.clients)) return true;

  return raw.clients.some((client: any) => {
    if (!client || typeof client.id !== 'string' || !client.id || !Array.isArray(client.rooms)) return true;
    if (['postalCode', 'street', 'complement', 'neighborhood', 'city', 'state', 'observations'].some(field => typeof client[field] !== 'string')) return true;
    return !Array.isArray(client.unassignedPhotos) || client.rooms.some((room: any) => {
      if (!room || typeof room.id !== 'string' || !room.id || !Array.isArray(room.records)) return true;
      if (!Array.isArray(room.photos)) return true;
      return room.records.some((record: any) =>
        !record || typeof record.id !== 'string' || !record.id || !Array.isArray(record.photos),
      );
    });
  });
}

function finiteNumber(value: unknown): number {
  const number = Number(value);
  return Number.isFinite(number) ? number : 0;
}

export function collectPhotos(clients: ClientData[]): PhotoItem[] {
  return clients.flatMap(client => [
    ...client.unassignedPhotos.map(photo => toPhotoItem(photo, client, '', '')),
    ...client.rooms.flatMap(room => [
      ...room.photos.map(photo => toPhotoItem(photo, client, isUnassignedRoom(room) ? '' : room.id, '')),
      ...room.records.flatMap(record => record.photos.map(photo => ({
        ...toPhotoItem(photo, client, room.id, record.createdAt),
        modelName: record.modelName,
        panelType: record.panelType,
        fabricMaterialName: record.fabricMaterialName,
        rodMaterialName: record.rodMaterialName,
        widthCentimeters: record.widthCentimeters,
        heightCentimeters: record.heightCentimeters,
      }))),
    ]),
  ]).sort((a, b) => (b.capturedAt || b.createdAt).localeCompare(a.capturedAt || a.createdAt));
}

export function assignClientPhotoToRoom(clients: ClientData[], clientId: string, path: string, roomId: string): ClientData[] {
  const client = clients.find(item => item.id === clientId);
  const room = client?.rooms.find(item => item.id === roomId);
  if (room && isUnassignedRoom(room)) throw new Error('Escolha o cômodo onde a foto será identificada.');
  const unassignedRoom = client?.rooms.find(isUnassignedRoom);
  const photo = client?.unassignedPhotos.find(item => item.path === path) || unassignedRoom?.photos.find(item => item.path === path);
  if (!client || !room || !photo) throw new Error('Não foi possível localizar a foto ou o cômodo.');
  return clients.map(item => item.id !== clientId ? item : {
    ...item,
    unassignedPhotos: item.unassignedPhotos.filter(candidate => candidate.path !== path),
    rooms: item.rooms.flatMap(candidate => {
      if (candidate.id === roomId) {
        const photos = candidate.photos.some(itemPhoto => itemPhoto.path === path)
          ? candidate.photos
          : [...candidate.photos, {...photo, roomName: candidate.name}];
        return [{...candidate, photos, imageLabel: `${photos.length} foto${photos.length === 1 ? '' : 's'}`}];
      }
      if (isUnassignedRoom(candidate)) {
        const photos = candidate.photos.filter(itemPhoto => itemPhoto.path !== path);
        return photos.length || candidate.records.length
          ? [{...candidate, photos, imageLabel: photos.length ? `${photos.length} foto${photos.length === 1 ? '' : 's'}` : 'Sem fotos'}]
          : [];
      }
      return [candidate];
    }),
  });
}

function normalizePhotos(value: unknown, roomName: unknown, recordId: unknown): PhotoData[] {
  return (Array.isArray(value) ? value : []).filter(Boolean).map((photo: any) => ({
    path: String(photo.path || ''),
    roomName: String(photo.roomName || roomName || ''),
    recordId: String(photo.recordId || recordId || ''),
    ...(photo.capturedAt ? {capturedAt: String(photo.capturedAt)} : {}),
  }));
}

function toPhotoItem(photo: PhotoData, client: ClientData, roomId: string, createdAt: string): PhotoItem {
  return {...photo, clientId: client.id, clientName: client.name, roomId, createdAt: photo.capturedAt || createdAt,
    roomName: photo.roomName || client.rooms.find(room => room.id === roomId)?.name || 'Sem cômodo'};
}

export function createVisitRecord(draft: VisitDraft, id = createId(), createdAt = new Date().toISOString()): VisitRecordData {
  return {
    id,
    createdAt,
    notes: draft.visitNotes.trim(),
    roomNotes: draft.roomNotes.trim(),
    openingName: draft.openingName.trim(),
    widthCentimeters: finiteNumber(draft.widthCentimeters),
    heightCentimeters: finiteNumber(draft.heightCentimeters),
    productCategory: draft.productCategory,
    modelName: draft.modelName,
    panelType: draft.panelType,
    fabricMaterialName: draft.fabricMaterialName,
    rodMaterialName: draft.rodMaterialName,
    photos: [],
  };
}

export function createEmptyDraft(client: ClientData, room: RoomData): VisitDraft {
  return {
    clientName: client.name,
    phone: client.phone,
    address: client.address,
    visitNotes: '',
    roomName: room.name,
    openingName: '',
    widthCentimeters: 0,
    heightCentimeters: 0,
    roomNotes: '',
    productCategory: 'Cortinas',
    modelName: 'CurtainPrefab',
    panelType: 'DoublePanel',
    fabricMaterialName: 'CurtainFabric',
    fabricMaterialPath: 'Assets/Materials/CurtainFabric.mat',
    rodMaterialName: 'RodMaterial',
    rodMaterialPath: 'Assets/Materials/RodMaterial.mat',
  };
}

export function seedClients(): ClientData[] {
  const room = (name: string): RoomData => ({id: createId(), name, imageLabel: 'Sem fotos', records: [], photos: []});
  return [
    {id: createId(), name: 'Maria Oliveira', phone: '(11) 99999-0000', address: 'Rua das Flores, 120', postalCode: '', street: 'Rua das Flores, 120', complement: '', neighborhood: '', city: '', state: '', observations: '', rooms: [room('Sala de estar'), room('Quarto casal')], unassignedPhotos: []},
    {id: createId(), name: 'Carlos Mendes', phone: '(11) 98888-1234', address: 'Av. Central, 45', postalCode: '', street: 'Av. Central, 45', complement: '', neighborhood: '', city: '', state: '', observations: '', rooms: [room('Quarto casal')], unassignedPhotos: []},
    {id: createId(), name: 'Ana Pereira', phone: '(11) 97777-4567', address: 'Rua do Sol, 89', postalCode: '', street: 'Rua do Sol, 89', complement: '', neighborhood: '', city: '', state: '', observations: '', rooms: [room('Escritório')], unassignedPhotos: []},
  ];
}
