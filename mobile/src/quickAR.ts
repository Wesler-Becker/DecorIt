import {NativeModules, Platform} from 'react-native';
import {loadLastARConfig} from './data/clientStorage';
import {UNASSIGNED_ROOM_NAME} from './data/clientDomain';
import type {ClientData, VisitDraft} from './types';

const fallbackConfig: VisitDraft = {
  clientName: '', phone: '', address: '', visitNotes: '', roomName: UNASSIGNED_ROOM_NAME,
  openingName: '', widthCentimeters: 0, heightCentimeters: 0, roomNotes: '',
  productCategory: 'Cortinas', modelName: 'CurtainPrefab', panelType: 'DoublePanel',
  fabricMaterialName: 'CurtainFabric', fabricMaterialPath: 'Assets/Materials/CurtainFabric.mat',
  rodMaterialName: 'RodMaterial', rodMaterialPath: 'Assets/Materials/RodMaterial.mat',
};

export async function openQuickAR(client: Pick<ClientData, 'id' | 'name' | 'phone' | 'address'>): Promise<void> {
  if (Platform.OS !== 'android') throw new Error('A câmera AR integrada está disponível no Android.');
  const savedConfig = await loadLastARConfig();
  const draft: VisitDraft = {
    ...fallbackConfig,
    ...savedConfig,
    clientName: client.name,
    phone: client.phone || '',
    address: client.address || '',
    visitNotes: '',
    roomName: UNASSIGNED_ROOM_NAME,
    roomNotes: '',
  };
  const module = NativeModules.UnityArModule;
  if (!module?.openAR) throw new Error('Este APK ainda não inclui a câmera AR integrada. Atualize o app Android.');
  await module.openAR({
    clientId: client.id,
    roomId: '',
    recordId: '',
    quickAR: true,
    draftJson: JSON.stringify(draft),
  });
}
