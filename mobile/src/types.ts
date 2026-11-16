export type PhotoData = {
  path: string;
  roomName: string;
  recordId: string;
  capturedAt?: string;
};

// Keep these property names aligned with the existing Unity JsonUtility model.
export type VisitRecordData = {
  id: string;
  createdAt: string;
  notes: string;
  roomNotes?: string;
  openingName: string;
  widthCentimeters: number;
  heightCentimeters: number;
  productCategory: string;
  modelName: string;
  panelType: 'SinglePanel' | 'DoublePanel' | string;
  fabricMaterialName: string;
  rodMaterialName: string;
  photos: PhotoData[];
};

export type RoomData = {
  id: string;
  name: string;
  imageLabel: string;
  records: VisitRecordData[];
  photos: PhotoData[];
};

export type ClientData = {
  id: string;
  name: string;
  phone: string;
  address: string;
  postalCode: string;
  street: string;
  complement: string;
  neighborhood: string;
  city: string;
  state: string;
  observations: string;
  rooms: RoomData[];
  unassignedPhotos: PhotoData[];
};

export type ClientDetails = Pick<ClientData, 'phone' | 'address' | 'postalCode' | 'street' | 'complement' | 'neighborhood' | 'city' | 'state' | 'observations'>;

export type CurtainLayerSettings = {
  widthCentimeters: number;
  heightCentimeters: number;
  panelType: 'SinglePanel' | 'DoublePanel';
  supportIndex: number;
  spacingMillimeters: number;
  fabricStyle: string;
  fabricMaterialPath: string;
  fabricMaterialName: string;
  foldsPerMeter: number;
  foldDepthCentimeters: number;
  foldProfile: number;
  naturalness: number;
  seed: number;
};

export type CurtainSettings = {
  modelName: string;
  supportType: 'Rod' | 'Rail';
  doubleSupport: boolean;
  opening: number;
  secondaryOpening: number;
  rodMaterialPath: string;
  rodMaterialName: string;
  layers: CurtainLayerSettings[];
};

export type VisitDraft = {
  clientName: string;
  phone: string;
  address: string;
  visitNotes: string;
  roomName: string;
  openingName: string;
  widthCentimeters: number;
  heightCentimeters: number;
  roomNotes: string;
  productCategory: string;
  modelName: string;
  panelType: 'SinglePanel' | 'DoublePanel';
  fabricMaterialName: string;
  fabricMaterialPath: string;
  rodMaterialName: string;
  rodMaterialPath: string;
  curtainSettings?: CurtainSettings;
};

export type PhotoItem = PhotoData & {
  clientId: string;
  clientName: string;
  roomId: string;
  createdAt: string;
  modelName?: string;
  panelType?: string;
  fabricMaterialName?: string;
  rodMaterialName?: string;
  widthCentimeters?: number;
  heightCentimeters?: number;
};

import type {NavigatorScreenParams} from '@react-navigation/native';

export type RootStackParamList = {
  MainTabs: NavigatorScreenParams<MainTabParamList> | undefined;
  ClientProfile: {clientId: string};
  ClientForm: {clientId?: string};
  NewRoom: {clientId: string; pendingPhotoPath?: string};
  RoomDetail: {clientId: string; roomId: string};
  RecordDetail: {clientId: string; roomId: string; recordId: string};
  NewRecord: {clientId: string; roomId?: string};
  CatalogSelection: undefined;
  UnityAR: {clientId: string; roomId?: string; recordId?: string; autoOpen?: boolean};
};

export type MainTabParamList = {
  Início: undefined;
  Clientes: undefined;
  'AR Rápido': undefined;
  Fotos: undefined;
  Catálogo: undefined;
};
