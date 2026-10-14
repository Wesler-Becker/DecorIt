import {assignClientPhotoToRoom, collectPhotos, createVisitRecord, needsClientCollectionMigration, normalizeClientCollection} from './clientDomain';
import type {VisitDraft} from '../types';

const legacyClient = {
  id: 'client-1',
  name: 'Maria',
  phone: '',
  address: '',
  rooms: [{
    id: 'room-1',
    name: 'Sala',
    imageLabel: '1 foto',
    records: [{
      id: 'record-1',
      createdAt: '2026-01-01T12:00:00.000Z',
      notes: 'Visita',
      roomNotes: 'Janela com pouco espaço lateral',
      openingName: 'Janela',
      widthCentimeters: '240',
      heightCentimeters: 180,
      productCategory: 'Cortinas',
      modelName: 'CurtainPrefab',
      panelType: 'DoublePanel',
      fabricMaterialName: 'CurtainFabric',
      rodMaterialName: 'RodMaterial',
      photos: [{path: '/files/sala.png', roomName: '', recordId: 'record-1'}],
    }],
  }],
};

describe('cliente e armazenamento legado', () => {
  it('normaliza dados Unity e preserva IDs, fotos e campos conhecidos', () => {
    const result = normalizeClientCollection({clients: [legacyClient]}).clients[0];
    expect(result.id).toBe('client-1');
    expect(result.rooms[0].records[0].widthCentimeters).toBe(240);
    expect(result.rooms[0].records[0].roomNotes).toBe('Janela com pouco espaço lateral');
    expect(result.rooms[0].records[0].photos[0].path).toBe('/files/sala.png');
  });

  it('persiste uma única vez os IDs gerados para cômodos do formato legado', () => {
    const oldCollection = {clients: [{name: 'Maria', rooms: [{name: 'Sala', imageLabel: 'Sem fotos'}]}]};
    expect(needsClientCollectionMigration(oldCollection)).toBe(true);

    const normalized = normalizeClientCollection(oldCollection);
    expect(needsClientCollectionMigration(normalized)).toBe(false);
    expect(normalizeClientCollection(normalized)).toEqual(normalized);
  });

  it('reúne fotos de todos os cômodos com o nome de cômodo como fallback', () => {
    const result = normalizeClientCollection({clients: [legacyClient]}).clients;
    expect(collectPhotos(result)).toEqual([expect.objectContaining({
      path: '/files/sala.png', clientName: 'Maria', roomName: 'Sala', roomId: 'room-1',
    })]);
  });

  it('preserva a data de captura individual da foto', () => {
    const result = normalizeClientCollection({clients: [{
      id: 'client-1', name: 'Maria', rooms: [{id: 'room-1', name: 'Sala', records: []}],
      unassignedPhotos: [{path: '/files/foto.png', roomName: 'Sem cômodo', recordId: '', capturedAt: '2026-10-14T09:30:00-03:00'}],
    }]});
    expect(collectPhotos(result.clients)[0].capturedAt).toBe('2026-10-14T09:30:00-03:00');
  });

  it('cria registro com notas do cômodo sem perder a estrutura compatível com Unity', () => {
    const draft: VisitDraft = {
      clientName: 'Maria', phone: '', address: '', visitNotes: '  Medição  ', roomName: 'Sala',
      openingName: 'Janela', widthCentimeters: 240, heightCentimeters: 180, roomNotes: '  Parede clara  ',
      productCategory: 'Cortinas', modelName: 'CurtainPrefab', panelType: 'DoublePanel',
      fabricMaterialName: 'CurtainFabric', fabricMaterialPath: 'Assets/Materials/CurtainFabric.mat',
      rodMaterialName: 'RodMaterial', rodMaterialPath: 'Assets/Materials/RodMaterial.mat',
    };
    const record = createVisitRecord(draft, 'record-new', '2026-01-02T12:00:00.000Z');
    expect(record).toMatchObject({id: 'record-new', notes: 'Medição', roomNotes: 'Parede clara', photos: []});
  });

  it('mantém fotos avulsas no cliente e permite vinculá-las a um cômodo depois', () => {
    const client = normalizeClientCollection({clients: [{
      id: 'client-1', name: 'Maria', rooms: [{id: 'room-1', name: 'Sala', records: []}],
      unassignedPhotos: [{path: '/files/ambiente.png', roomName: 'Sem cômodo', recordId: ''}],
    }]}).clients;
    expect(collectPhotos(client)[0]).toMatchObject({path: '/files/ambiente.png', roomId: ''});

    const assigned = assignClientPhotoToRoom(client, 'client-1', '/files/ambiente.png', 'room-1');
    expect(assigned[0].unassignedPhotos).toHaveLength(0);
    expect(assigned[0].rooms[0].photos).toEqual([{path: '/files/ambiente.png', roomName: 'Sala', recordId: ''}]);
    expect(collectPhotos(assigned)[0]).toMatchObject({path: '/files/ambiente.png', roomId: 'room-1', roomName: 'Sala'});
  });
});
