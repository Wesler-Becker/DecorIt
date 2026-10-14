import React, {useCallback, useEffect, useMemo, useState} from 'react';
import {
  Alert,
  AppState,
  FlatList,
  Image,
  Modal,
  NativeModules,
  Platform,
  Pressable,
  RefreshControl,
  StyleSheet,
  Text,
  TextInput,
  View,
  useWindowDimensions,
} from 'react-native';
import {useFocusEffect, useNavigation, useRoute} from '@react-navigation/native';
import type {NativeStackNavigationProp} from '@react-navigation/native-stack';
import {
  ArrowRight,
  Camera,
  Check,
  ChevronRight,
  ClipboardList,
  DoorOpen,
  Ellipsis,
  Image as ImageIcon,
  Images,
  MapPin,
  Pencil,
  Plus,
  Search,
  Sofa,
  Sparkles,
  UserRound,
  Users,
  X,
} from 'lucide-react-native';
import {Button, Card, EmptyState, LinkRow, Page, Pill, SectionTitle, TextField} from './components/UI';
import {collectPhotos, createEmptyDraft} from './data/clientDomain';
import {photoFileUri} from './data/clientStorage';
import {useAppState, useVisitDraft} from './state/AppState';
import {colors, radius} from './theme';
import type {ClientData, MainTabParamList, PhotoItem, RootStackParamList, RoomData} from './types';

type Nav = NativeStackNavigationProp<RootStackParamList>;
const listKey = (item: {id: string}) => item.id;

export function HomeScreen() {
  const navigation = useNavigation<Nav>();
  const {clients, ready, reload} = useAppState();
  const recentRecords = useMemo(() => clients.flatMap(client => client.rooms.flatMap(room => room.records.map(record => ({client, room, record}))))
    .sort((a, b) => b.record.createdAt.localeCompare(a.record.createdAt)).slice(0, 3), [clients]);

  useFocusEffect(useCallback(() => { void reload(); }, [reload]));

  return (
    <Page title="DecorIt" subtitle="Projetos e visualização de ambientes">
      <View style={styles.welcomeRow}>
        <View><Text style={styles.eyebrow}>SEU ESPAÇO DE TRABALHO</Text><Text style={styles.welcomeTitle}>Olá! Vamos planejar{ '\n' }um novo ambiente?</Text></View>
        <View style={styles.welcomeIcon}><Sofa size={26} color={colors.blue} /></View>
      </View>

      <Pressable onPress={() => navigation.navigate('MainTabs', {screen: 'Clientes'})} style={({pressed}) => [styles.hero, pressed && styles.pressed]}>
        <View style={styles.heroTop}><View style={styles.heroIcon}><Sparkles size={22} color={colors.gold} /></View><Text style={styles.heroTag}>VISUALIZAÇÃO EM AR</Text></View>
        <Text style={styles.heroTitle}>Veja cortinas e persianas{ '\n'}no espaço do cliente</Text>
        <View style={styles.heroAction}><Text style={styles.heroActionText}>Abrir clientes</Text><ArrowRight size={22} color="#FFFFFF" /></View>
      </Pressable>

      <View style={styles.statRow}>
        <Card style={styles.statCard}><View style={[styles.statIcon, {backgroundColor: colors.bluePale}]}><Users size={22} color={colors.blue} /></View><Text style={styles.statValue}>{ready ? clients.length : '—'}</Text><Text style={styles.statLabel}>clientes</Text></Card>
        <Card style={styles.statCard}><View style={[styles.statIcon, {backgroundColor: colors.goldPale}]}><ClipboardList size={22} color="#9A7429" /></View><Text style={styles.statValue}>{clients.reduce((n, client) => n + client.rooms.reduce((sum, room) => sum + room.records.length, 0), 0)}</Text><Text style={styles.statLabel}>registros</Text></Card>
        <Card style={styles.statCard}><View style={[styles.statIcon, {backgroundColor: '#E9F3EE'}]}><DoorOpen size={22} color={colors.green} /></View><Text style={styles.statValue}>{clients.reduce((n, client) => n + client.rooms.length, 0)}</Text><Text style={styles.statLabel}>cômodos</Text></Card>
      </View>

      <SectionTitle trailing={<Pressable onPress={() => navigation.navigate('MainTabs', {screen: 'Clientes'})}><Text style={styles.seeAll}>Ver clientes</Text></Pressable>}>Acesso rápido</SectionTitle>
      <Card style={styles.actionCard}>
        <LinkRow label="Cadastrar cliente" detail="Comece pelo nome; os outros dados são opcionais" onPress={() => navigation.navigate('ClientForm', {})} />
        <View style={styles.divider} />
        <LinkRow label="Modelos e materiais" detail="Cortinas, persianas e acessórios" onPress={() => navigation.navigate('MainTabs', {screen: 'Catálogo'})} />
        <View style={styles.divider} />
        <LinkRow label="Todas as fotos" detail="Imagens registradas em qualquer cômodo" onPress={() => navigation.navigate('MainTabs', {screen: 'Fotos'})} />
      </Card>

      <SectionTitle>Acompanhe os registros</SectionTitle>
      {!ready ? <Card><Text style={styles.muted}>Carregando seus dados…</Text></Card> : recentRecords.length === 0 ? (
        <Card><EmptyState title="Nenhum registro ainda" description="Os registros criados nos cômodos dos clientes aparecerão aqui." /></Card>
      ) : recentRecords.map(({client, room, record}) => (
        <Pressable key={record.id} onPress={() => navigation.navigate('RoomDetail', {clientId: client.id, roomId: room.id})} style={({pressed}) => [styles.recentCard, pressed && styles.pressed]}>
          <View style={styles.recentIcon}><ClipboardList size={22} color={colors.blue} /></View>
          <View style={styles.recentCopy}><Text style={styles.rowTitle}>{client.name}</Text><Text style={styles.rowDetail}>{room.name} · {record.modelName}</Text></View>
          <ChevronRight size={22} color={colors.muted} />
        </Pressable>
      ))}
    </Page>
  );
}

export function ClientsScreen() {
  const navigation = useNavigation<Nav>();
  const {clients, ready, reload} = useAppState();
  const [query, setQuery] = useState('');
  const [activeClient, setActiveClient] = useState<ClientData | null>(null);
  const [chooseRoom, setChooseRoom] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  useFocusEffect(useCallback(() => { void reload(); }, [reload]));

  const filteredClients = useMemo(() => {
    const search = query.trim().toLocaleLowerCase('pt-BR');
    if (!search) return clients;
    return clients.filter(client => `${client.name} ${client.phone} ${client.address}`.toLocaleLowerCase('pt-BR').includes(search));
  }, [clients, query]);

  const startRecord = (client: ClientData, room?: RoomData) => {
    setActiveClient(null);
    setChooseRoom(false);
    if (room) navigation.navigate('NewRecord', {clientId: client.id, roomId: room.id});
    else if (client.rooms.length === 1) navigation.navigate('NewRecord', {clientId: client.id, roomId: client.rooms[0].id});
    else if (client.rooms.length === 0) {
      Alert.alert('Cadastre um cômodo', 'Para criar um registro, primeiro adicione um cômodo ao perfil deste cliente.', [
        {text: 'Agora não', style: 'cancel'},
        {text: 'Criar cômodo', onPress: () => navigation.navigate('NewRoom', {clientId: client.id})},
      ]);
    } else setChooseRoom(true);
  };

  return (
    <Page title="Clientes" subtitle={`${clients.length} ${clients.length === 1 ? 'cliente cadastrado' : 'clientes cadastrados'}`} scroll={false} action={
      <Pressable onPress={() => navigation.navigate('ClientForm', {})} style={styles.addHeaderButton} accessibilityRole="button" accessibilityLabel="Novo cliente"><Plus size={22} color="#FFFFFF" /></Pressable>
    }>
      <FlatList
        data={filteredClients}
        keyExtractor={listKey}
        contentContainerStyle={styles.listContent}
        keyboardShouldPersistTaps="handled"
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={async () => {setRefreshing(true); await reload(); setRefreshing(false);}} tintColor={colors.blue} />}
        ListHeaderComponent={<View style={styles.listHeader}>
          <View style={styles.searchBox}><Search size={22} color={colors.muted} /><TextInput value={query} onChangeText={setQuery} placeholder="Buscar nome, telefone ou endereço" placeholderTextColor="#99A5AC" style={styles.searchInput} clearButtonMode="while-editing" /></View>
          <Text style={styles.listHint}>Toque em um cliente para abrir o perfil. Use ⋯ para outras ações.</Text>
        </View>}
        ListEmptyComponent={ready ? <Card><EmptyState title={query ? 'Nenhum resultado' : 'Sua lista começa aqui'} description={query ? 'Tente buscar por outro nome ou telefone.' : 'Cadastre o primeiro cliente para organizar cômodos, registros e fotos.'} action={<Button label="Cadastrar cliente" icon={<Plus size={22} color="#FFFFFF" />} onPress={() => navigation.navigate('ClientForm', {})} />} /></Card> : <Card><Text style={styles.muted}>Carregando clientes…</Text></Card>}
        ItemSeparatorComponent={() => <View style={{height: 10}} />}
        renderItem={({item}) => <ClientCard client={item} onOpen={() => navigation.navigate('ClientProfile', {clientId: item.id})} onActions={() => {setActiveClient(item); setChooseRoom(false);}} />}
      />
      <Modal visible={Boolean(activeClient)} transparent animationType="fade" onRequestClose={() => {setActiveClient(null); setChooseRoom(false);}}>
        <Pressable style={styles.modalBackdrop} onPress={() => {setActiveClient(null); setChooseRoom(false);}}>
          <Pressable style={styles.actionSheet} onPress={event => event.stopPropagation()}>
            <View style={styles.sheetHandle} />
            {activeClient ? chooseRoom ? <>
              <View style={styles.sheetTitleRow}><Text style={styles.sheetTitle}>Escolha o cômodo</Text><Pressable onPress={() => setChooseRoom(false)}><X size={21} color={colors.muted} /></Pressable></View>
              {activeClient.rooms.map(room => <LinkRow key={room.id} label={room.name} detail="Criar um registro neste cômodo" onPress={() => startRecord(activeClient, room)} />)}
            </> : <>
              <Text style={styles.sheetTitle}>{activeClient.name}</Text>
              <LinkRow label="Abrir perfil" detail="Dados, cômodos e fotos" onPress={() => {const client = activeClient; setActiveClient(null); navigation.navigate('ClientProfile', {clientId: client.id});}} />
              <LinkRow label="Editar cliente" detail="Atualizar nome e dados de contato" onPress={() => {const client = activeClient; setActiveClient(null); navigation.navigate('ClientForm', {clientId: client.id});}} />
              <LinkRow label="Criar registro" detail={activeClient.rooms.length ? 'Escolher um cômodo do cliente' : 'Adicione primeiro um cômodo'} onPress={() => startRecord(activeClient)} />
            </> : null}
          </Pressable>
        </Pressable>
      </Modal>
    </Page>
  );
}

function ClientCard({client, onOpen, onActions}: {client: ClientData; onOpen: () => void; onActions: () => void}) {
  return (
    <Card style={styles.clientCard}>
      <Pressable onPress={onOpen} style={({pressed}) => [styles.clientOpen, pressed && styles.pressed]} accessibilityRole="button" accessibilityLabel={`Abrir perfil de ${client.name}`}>
        <View style={styles.avatar}><UserRound size={21} color={colors.blue} /></View>
        <View style={styles.clientCopy}>
          <Text numberOfLines={1} style={styles.rowTitle}>{client.name}</Text>
          <Text numberOfLines={1} style={styles.rowDetail}>{client.phone || 'Telefone não informado'}</Text>
          <Text numberOfLines={1} style={styles.rowDetail}>{client.rooms.length} {client.rooms.length === 1 ? 'cômodo' : 'cômodos'}</Text>
        </View>
        <ChevronRight size={22} color={colors.muted} />
      </Pressable>
      <View style={styles.clientCardFooter}>
        <View style={styles.addressLine}><MapPin size={14} color={colors.muted} /><Text numberOfLines={1} style={styles.addressText}>{client.address || 'Endereço não informado'}</Text></View>
        <Pressable onPress={event => {event.stopPropagation(); onActions();}} hitSlop={8} style={styles.moreButton} accessibilityRole="button" accessibilityLabel={`Ações de ${client.name}`}><Ellipsis size={21} color={colors.ink} /></Pressable>
      </View>
    </Card>
  );
}

export function ClientFormScreen() {
  const navigation = useNavigation<Nav>();
  const route = useRoute<any>();
  const clientId = route.params?.clientId as string | undefined;
  const {clients, addClient, updateClient} = useAppState();
  const existing = clients.find(client => client.id === clientId);
  const [name, setName] = useState(existing?.name || '');
  const [phone, setPhone] = useState(existing?.phone || '');
  const [address, setAddress] = useState(existing?.address || '');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const save = async () => {
    if (!name.trim()) {setError('O nome é obrigatório.'); return;}
    setSaving(true); setError('');
    try {
      if (clientId) {
        await updateClient(clientId, {name, phone, address});
        navigation.goBack();
      } else {
        const client = await addClient(name, phone, address);
        navigation.replace('ClientProfile', {clientId: client.id});
      }
    } catch {setError('Não foi possível salvar. Tente novamente.');}
    finally {setSaving(false);}
  };

  return <Page title={clientId ? 'Editar cliente' : 'Novo cliente'} subtitle="O nome é o único dado obrigatório" navigation={navigation}>
    <Card style={styles.formCard}>
      <View style={styles.formIntro}><View style={styles.avatarLarge}><UserRound size={23} color={colors.blue} /></View><View style={{flex: 1}}><Text style={styles.rowTitle}>{clientId ? 'Atualize o cadastro' : 'Vamos começar pelo nome'}</Text><Text style={styles.rowDetail}>Telefone e endereço podem ser adicionados depois.</Text></View></View>
      <TextField label="Nome do cliente" value={name} onChangeText={setName} placeholder="Ex.: Mariana Oliveira" required />
      <TextField label="Telefone" value={phone} onChangeText={setPhone} placeholder="(00) 00000-0000" keyboardType="phone-pad" />
      <TextField label="Endereço" value={address} onChangeText={setAddress} placeholder="Rua, número, bairro" multiline />
      {error ? <Text style={styles.errorText}>{error}</Text> : null}
      <Button label={clientId ? 'Salvar alterações' : 'Cadastrar cliente'} onPress={save} loading={saving} icon={clientId ? <Check size={22} color="#FFFFFF" /> : <Plus size={22} color="#FFFFFF" />} />
    </Card>
  </Page>;
}

export function ClientProfileScreen() {
  const navigation = useNavigation<Nav>();
  const route = useRoute<any>();
  const clientId = route.params?.clientId as string;
  const {clients, reload, assignUnassignedPhoto} = useAppState();
  const [section, setSection] = useState<'rooms' | 'photos'>('rooms');
  const [photoToAssign, setPhotoToAssign] = useState<PhotoItem | null>(null);
  useFocusEffect(useCallback(() => { void reload(); }, [reload]));
  const client = clients.find(item => item.id === clientId);
  const photos = useMemo(() => client ? collectPhotos([client]) : [], [client]);

  if (!client) return <Page title="Perfil do cliente" navigation={navigation}><EmptyState title="Cliente não encontrado" description="Volte à lista e selecione um cadastro existente." /></Page>;

  return <Page title="Perfil do cliente" subtitle="Dados, cômodos e fotos" navigation={navigation}>
    <Card style={styles.profileCard}>
      <View style={styles.profileHead}><View style={styles.avatarLarge}><UserRound size={24} color={colors.blue} /></View><View style={{flex: 1}}><Text style={styles.profileName}>{client.name}</Text><Text style={styles.rowDetail}>{client.phone || 'Telefone não informado'}</Text></View><Pressable onPress={() => navigation.navigate('ClientForm', {clientId})} style={styles.editIconButton} accessibilityRole="button" accessibilityLabel="Editar cliente"><Pencil size={22} color={colors.blue} /></Pressable></View>
      {client.address ? <View style={styles.profileAddress}><MapPin size={16} color={colors.muted} /><Text style={styles.addressText}>{client.address}</Text></View> : <Text style={styles.muted}>Endereço ainda não informado</Text>}
      <View style={styles.profileStats}><Text style={styles.profileStat}><Text style={styles.profileStatNum}>{client.rooms.length}</Text> cômodos</Text><View style={styles.verticalDivider} /><Text style={styles.profileStat}><Text style={styles.profileStatNum}>{photos.length}</Text> fotos</Text></View>
    </Card>
    <Button label="Registrar foto do cliente" variant="secondary" icon={<Camera size={22} color={colors.blue} />} onPress={() => navigation.navigate('UnityAR', {clientId})} />
    <View style={styles.segmented}>
      <Pressable onPress={() => setSection('rooms')} style={[styles.segment, section === 'rooms' && styles.segmentActive]}><Text style={[styles.segmentText, section === 'rooms' && styles.segmentTextActive]}>Cômodos</Text></Pressable>
      <Pressable onPress={() => setSection('photos')} style={[styles.segment, section === 'photos' && styles.segmentActive]}><Text style={[styles.segmentText, section === 'photos' && styles.segmentTextActive]}>Fotos ({photos.length})</Text></Pressable>
    </View>
    {section === 'rooms' ? <>
      <SectionTitle trailing={<Pressable onPress={() => navigation.navigate('NewRoom', {clientId})} style={styles.inlineAdd}><Plus size={15} color={colors.blue} /><Text style={styles.seeAll}>Novo cômodo</Text></Pressable>}>Ambientes</SectionTitle>
      {client.rooms.length === 0 ? <Card><EmptyState title="Nenhum cômodo cadastrado" description="Crie um cômodo para organizar medidas, registros e fotos deste cliente." action={<Button label="Criar cômodo" icon={<Plus size={22} color="#FFFFFF" />} onPress={() => navigation.navigate('NewRoom', {clientId})} />} /></Card> : client.rooms.map(room => <RoomCard key={room.id} room={room} onPress={() => navigation.navigate('RoomDetail', {clientId, roomId: room.id})} />)}
    </> : photos.length === 0 ? <Card><EmptyState title="Nenhuma foto registrada" description="As imagens feitas durante a visualização em AR aparecerão aqui, sem distinção de cômodo." /></Card> : <PhotoGrid photos={photos} onOpenRoom={photo => photo.roomId ? navigation.navigate('RoomDetail', {clientId: photo.clientId, roomId: photo.roomId}) : undefined} onOpenRecord={photo => photo.roomId && photo.recordId ? navigation.navigate('RecordDetail', {clientId: photo.clientId, roomId: photo.roomId, recordId: photo.recordId}) : undefined} onIdentify={photo => setPhotoToAssign(photo)} />}
    <PhotoRoomAssignmentModal photo={photoToAssign} rooms={client.rooms} onClose={() => setPhotoToAssign(null)} onSelect={room => {if (photoToAssign) void assignUnassignedPhoto(photoToAssign.clientId, photoToAssign.path, room.id); setPhotoToAssign(null);}} onCreateRoom={() => {navigation.navigate('NewRoom', {clientId}); setPhotoToAssign(null);}} />
  </Page>;
}

function RoomCard({room, onPress}: {room: RoomData; onPress: () => void}) {
  const photoCount = room.photos.length + room.records.reduce((n, record) => n + record.photos.length, 0);
  return <Pressable onPress={onPress} style={({pressed}) => [styles.roomCard, pressed && styles.pressed]}>
    <View style={styles.roomIcon}><DoorOpen size={21} color={colors.blue} /></View>
    <View style={{flex: 1}}><Text style={styles.rowTitle}>{room.name}</Text><Text style={styles.rowDetail}>{room.records.length} {room.records.length === 1 ? 'registro' : 'registros'} · {photoCount} {photoCount === 1 ? 'foto' : 'fotos'}</Text></View>
    <ChevronRight size={22} color={colors.muted} />
  </Pressable>;
}

export function NewRoomScreen() {
  const navigation = useNavigation<Nav>();
  const route = useRoute<any>();
  const clientId = route.params?.clientId as string;
  const client = useAppState().clients.find(item => item.id === clientId);
  const {addRoom} = useAppState();
  const [name, setName] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const save = async () => {
    if (!name.trim()) {setError('Informe o nome do cômodo.'); return;}
    if (!client) {setError('Cliente não encontrado.'); return;}
    setSaving(true);
    try {await addRoom(clientId, name); navigation.goBack();}
    catch {setError('Não foi possível cadastrar o cômodo.');}
    finally {setSaving(false);}
  };
  return <Page title="Novo cômodo" subtitle={client?.name || 'Cliente'} navigation={navigation}>
    <Card style={styles.formCard}><View style={styles.formIntro}><View style={styles.roomIcon}><DoorOpen size={22} color={colors.blue} /></View><View style={{flex: 1}}><Text style={styles.rowTitle}>Qual ambiente vamos organizar?</Text><Text style={styles.rowDetail}>O registro e as fotos ficarão associados a este cômodo.</Text></View></View>
      <TextField label="Nome do cômodo" value={name} onChangeText={setName} placeholder="Ex.: Sala de estar" required />
      {error ? <Text style={styles.errorText}>{error}</Text> : null}<Button label="Criar cômodo" icon={<Plus size={22} color="#FFFFFF" />} onPress={save} loading={saving} />
    </Card>
  </Page>;
}

export function RoomDetailScreen() {
  const navigation = useNavigation<Nav>();
  const route = useRoute<any>();
  const {clientId, roomId} = route.params as {clientId: string; roomId: string};
  const {clients, reload} = useAppState();
  useFocusEffect(useCallback(() => { void reload(); }, [reload]));
  const client = clients.find(item => item.id === clientId);
  const room = client?.rooms.find(item => item.id === roomId);
  const records = useMemo(() => [...(room?.records || [])].sort((a, b) => b.createdAt.localeCompare(a.createdAt)), [room]);
  const photos = useMemo(() => room ? [
    ...room.photos.map(photo => ({...photo, createdAt: '', clientId, clientName: client?.name || '', roomId})),
    ...records.flatMap(record => record.photos.map(photo => ({
      ...photo, createdAt: record.createdAt, clientId, clientName: client?.name || '', roomId,
      modelName: record.modelName, panelType: record.panelType, fabricMaterialName: record.fabricMaterialName,
      rodMaterialName: record.rodMaterialName, widthCentimeters: record.widthCentimeters, heightCentimeters: record.heightCentimeters,
    }))),
  ] : [], [room, records, client?.name, clientId, roomId]);
  const create = () => navigation.navigate('NewRecord', {clientId, roomId});

  if (!client || !room) return <Page title="Cômodo" navigation={navigation}><EmptyState title="Cômodo não encontrado" description="Volte ao perfil do cliente e selecione outro ambiente." /></Page>;

  return <Page title={room.name} subtitle={client.name} navigation={navigation} action={<Pressable onPress={create} style={styles.addHeaderButton} accessibilityRole="button" accessibilityLabel="Criar registro"><Plus size={22} color="#FFFFFF" /></Pressable>}>
    <View style={styles.roomSummary}><View style={styles.roomIcon}><DoorOpen size={22} color={colors.blue} /></View><View style={{flex: 1}}><Text style={styles.rowTitle}>Registros deste ambiente</Text><Text style={styles.rowDetail}>Medidas, modelo escolhido e fotos ficam vinculados aqui.</Text></View></View>
    <Button label="Criar registro neste cômodo" icon={<Plus size={22} color="#FFFFFF" />} onPress={create} />
    <SectionTitle trailing={<Text style={styles.countBadge}>{records.length}</Text>}>Histórico</SectionTitle>
    {records.length === 0 ? <Card><EmptyState title="Nenhum registro neste cômodo" description="Comece uma visita para salvar medidas, produto escolhido e imagens da visualização em AR." action={<Button label="Criar primeiro registro" onPress={create} icon={<Plus size={22} color="#FFFFFF" />} />} /></Card> : records.map(record => <RecordCard key={record.id} record={record} onPress={() => navigation.navigate('RecordDetail', {clientId, roomId, recordId: record.id})} />)}
    <SectionTitle trailing={<Text style={styles.countBadge}>{photos.length}</Text>}>Fotos do cômodo</SectionTitle>
    {photos.length ? <PhotoGrid photos={photos} onOpenRecord={photo => photo.recordId ? navigation.navigate('RecordDetail', {clientId, roomId, recordId: photo.recordId}) : undefined} /> : <Card><Text style={styles.muted}>As fotos feitas em AR aparecerão neste cômodo e também na aba Fotos.</Text></Card>}
  </Page>;
}

export function RecordDetailScreen() {
  const navigation = useNavigation<Nav>();
  const route = useRoute<any>();
  const {clientId, roomId, recordId} = route.params as {clientId: string; roomId: string; recordId: string};
  const {clients, reload, updateRecord} = useAppState();
  useFocusEffect(useCallback(() => { void reload(); }, [reload]));
  const client = clients.find(item => item.id === clientId);
  const room = client?.rooms.find(item => item.id === roomId);
  const record = room?.records.find(item => item.id === recordId);
  if (!client || !room || !record) return <Page title="Registro" navigation={navigation}><EmptyState title="Registro não encontrado" description="Volte ao cômodo e selecione outro registro." /></Page>;
  const photos: PhotoItem[] = record.photos.map(photo => ({
    ...photo, clientId, clientName: client.name, roomId, roomName: photo.roomName || room.name, createdAt: record.createdAt,
    modelName: record.modelName, panelType: record.panelType, fabricMaterialName: record.fabricMaterialName,
    rodMaterialName: record.rodMaterialName, widthCentimeters: record.widthCentimeters, heightCentimeters: record.heightCentimeters,
  }));
  const date = record.createdAt ? new Date(record.createdAt) : null;
  const dateLabel = date && !Number.isNaN(date.valueOf()) ? date.toLocaleDateString('pt-BR', {day: '2-digit', month: 'long', year: 'numeric'}) : 'Registro';
  return <Page title="Registro" subtitle={`${client.name} · ${room.name}`} navigation={navigation}>
    <Card style={styles.formCard}>
      <Text style={styles.formTitle}>{dateLabel}</Text>
      <EditableRecordField label="Abertura" value={record.openingName} placeholder="Ex.: Janela principal" onSave={value => updateRecord(clientId, roomId, recordId, {openingName: value})} />
      <EditableRecordField label="Observações da visita" value={record.notes} placeholder="Observações da visita" multiline onSave={value => updateRecord(clientId, roomId, recordId, {notes: value})} />
      <SummaryRow label="Medidas" value={`${record.widthCentimeters || '—'} × ${record.heightCentimeters || '—'} cm`} />
      <SummaryRow label="Modelo" value={`${record.modelName} · ${record.panelType === 'SinglePanel' ? 'Painel único' : 'Painel duplo'}`} />
      <SummaryRow label="Tecido" value={record.fabricMaterialName || 'Não informado'} />
      <SummaryRow label="Varão" value={record.rodMaterialName || 'Não informado'} />
      {record.roomNotes ? <SummaryRow label="Observações do cômodo" value={record.roomNotes} /> : null}
      <Button label="Abrir visualização AR" icon={<Camera size={22} color="#FFFFFF" />} onPress={() => navigation.navigate('UnityAR', {clientId, roomId, recordId})} />
    </Card>
    <SectionTitle trailing={<Text style={styles.countBadge}>{photos.length}</Text>}>Fotos deste registro</SectionTitle>
    {photos.length ? <PhotoGrid photos={photos} /> : <Card><Text style={styles.muted}>As fotos capturadas durante esta visualização em AR aparecerão aqui.</Text></Card>}
  </Page>;
}

function EditableRecordField({label, value, placeholder, multiline = false, onSave}: {label: string; value: string; placeholder: string; multiline?: boolean; onSave: (value: string) => Promise<void>}) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(value || '');
  const startEditing = () => {
    setDraft(value || '');
    setEditing(true);
  };
  const cancelEditing = () => {
    setDraft(value || '');
    setEditing(false);
  };
  const save = async () => {
    try {
      await onSave(draft);
      setEditing(false);
    } catch (cause) {
      Alert.alert('Não foi possível salvar', cause instanceof Error ? cause.message : 'Tente novamente.');
    }
  };
  return <View style={styles.summaryRow}>
    <View style={styles.editableRecordHeader}>{editing
      ? <View style={styles.editableRecordInput}><TextField label={label} value={draft} onChangeText={setDraft} placeholder={placeholder} multiline={multiline} /></View>
      : <View style={styles.editableRecordValue}><Text style={styles.summaryLabel}>{label}</Text><Text style={styles.summaryValue}>{value || (label === 'Abertura' ? 'Não informada' : 'Nenhuma')}</Text></View>}
      <View style={styles.editableRecordActions}>
      {editing ? <>
        <Pressable onPress={() => { void save(); }} style={styles.editableRecordIcon} accessibilityRole="button" accessibilityLabel={`Salvar ${label}`}><Check size={21} color={colors.green} /></Pressable>
        <Pressable onPress={cancelEditing} style={styles.editableRecordIcon} accessibilityRole="button" accessibilityLabel={`Cancelar edição de ${label}`}><X size={21} color={colors.danger} /></Pressable>
      </> : <Pressable onPress={startEditing} style={styles.editableRecordIcon} accessibilityRole="button" accessibilityLabel={`Editar ${label}`}><Pencil size={19} color={colors.blue} /></Pressable>}
    </View></View>
  </View>;
}

function RecordCard({record, onPress}: {record: ClientData['rooms'][number]['records'][number]; onPress: () => void}) {
  const date = record.createdAt ? new Date(record.createdAt) : null;
  const dateLabel = date && !Number.isNaN(date.valueOf()) ? date.toLocaleDateString('pt-BR', {day: '2-digit', month: 'short', year: 'numeric'}) : 'Registro';
  return <Pressable onPress={onPress} style={({pressed}) => [styles.recordCard, pressed && styles.pressed]}>
    <View style={styles.recordIcon}><ClipboardList size={22} color={colors.blue} /></View>
    <View style={{flex: 1, gap: 5}}><View style={styles.recordTop}><Text style={styles.rowTitle}>{dateLabel}</Text><Text style={styles.recordPhotoCount}>{record.photos.length} {record.photos.length === 1 ? 'foto' : 'fotos'}</Text></View>
      <Text style={styles.rowDetail}>{record.openingName || 'Abertura não informada'} · {record.widthCentimeters || '—'} × {record.heightCentimeters || '—'} cm</Text>
      <Text style={styles.rowDetail}>{record.modelName} · {record.panelType === 'SinglePanel' ? 'Painel único' : 'Painel duplo'}</Text>
      {record.notes ? <Text style={styles.recordNotes} numberOfLines={2}>{record.notes}</Text> : null}
    </View><ChevronRight size={22} color={colors.muted} />
  </Pressable>;
}

export function PhotosScreen() {
  const navigation = useNavigation<Nav>();
  const {clients, reload, assignUnassignedPhoto} = useAppState();
  const [photoToAssign, setPhotoToAssign] = useState<PhotoItem | null>(null);
  useFocusEffect(useCallback(() => { void reload(); }, [reload]));
  const photos = useMemo(() => collectPhotos(clients), [clients]);
  const rooms = clients.find(client => client.id === photoToAssign?.clientId)?.rooms || [];
  return <Page title="Fotos" subtitle="Imagens de todos os clientes, com ou sem cômodo" scroll={false}>
    {photos.length === 0 ? <View style={styles.emptyFill}><EmptyState title="Nenhuma foto por enquanto" description="As imagens capturadas durante a visualização em AR serão reunidas aqui, com acesso ao cômodo de origem." /></View> : <FlatList data={photos} keyExtractor={(item, index) => `${item.path}-${index}`} numColumns={2} contentContainerStyle={styles.photoList} columnWrapperStyle={styles.photoRow} renderItem={({item, index}) => <PhotoTile photo={item} photos={photos} initialIndex={index} onOpenRoom={photo => photo.roomId ? navigation.navigate('RoomDetail', {clientId: photo.clientId, roomId: photo.roomId}) : undefined} onOpenRecord={photo => photo.roomId && photo.recordId ? navigation.navigate('RecordDetail', {clientId: photo.clientId, roomId: photo.roomId, recordId: photo.recordId}) : undefined} onIdentify={photo => setPhotoToAssign(photo)} />} />}
    <PhotoRoomAssignmentModal photo={photoToAssign} rooms={rooms} onClose={() => setPhotoToAssign(null)} onSelect={room => {if (photoToAssign) void assignUnassignedPhoto(photoToAssign.clientId, photoToAssign.path, room.id); setPhotoToAssign(null);}} onCreateRoom={() => {if (photoToAssign) navigation.navigate('NewRoom', {clientId: photoToAssign.clientId}); setPhotoToAssign(null);}} />
  </Page>;
}

function PhotoRoomAssignmentModal({photo, rooms, onClose, onSelect, onCreateRoom}: {photo: PhotoItem | null; rooms: RoomData[]; onClose: () => void; onSelect: (room: RoomData) => void; onCreateRoom: () => void}) {
  return <Modal visible={Boolean(photo)} transparent animationType="slide" onRequestClose={onClose}>
    <Pressable style={styles.modalBackdrop} onPress={onClose}>
      <Pressable style={styles.actionSheet} onPress={event => event.stopPropagation()}>
        <View style={styles.sheetHandle} />
        <View style={styles.sheetTitleRow}><Text style={styles.sheetTitle}>Identificar cômodo</Text><Pressable onPress={onClose} accessibilityRole="button" accessibilityLabel="Fechar"><X size={21} color={colors.muted} /></Pressable></View>
        <Text style={styles.rowDetail}>A foto continuará no perfil do cliente e será exibida também no cômodo escolhido.</Text>
        {rooms.length ? rooms.map(room => <LinkRow key={room.id} label={room.name} detail="Vincular esta foto ao cômodo" onPress={() => onSelect(room)} />) : <Text style={styles.muted}>Este cliente ainda não tem cômodos.</Text>}
        <Button label="Criar cômodo" variant="secondary" icon={<Plus size={22} color={colors.blue} />} onPress={onCreateRoom} />
      </Pressable>
    </Pressable>
  </Modal>;
}

function PhotoGrid({photos, onOpenRoom, onOpenRecord, onIdentify}: {photos: PhotoItem[]; onOpenRoom?: (photo: PhotoItem) => void; onOpenRecord?: (photo: PhotoItem) => void; onIdentify?: (photo: PhotoItem) => void}) {
  return <View style={styles.photoGrid}>{photos.map((photo, index) => <View key={`${photo.path}-${index}`} style={styles.photoGridCell}><PhotoTile photo={photo} photos={photos} initialIndex={index} onOpenRoom={onOpenRoom} onOpenRecord={onOpenRecord} onIdentify={onIdentify} /></View>)}</View>;
}

function PhotoTile({photo, photos, initialIndex, onOpenRoom, onOpenRecord, onIdentify}: {photo: PhotoItem; photos: PhotoItem[]; initialIndex: number; onOpenRoom?: (photo: PhotoItem) => void; onOpenRecord?: (photo: PhotoItem) => void; onIdentify?: (photo: PhotoItem) => void}) {
  const [previewVisible, setPreviewVisible] = useState(false);
  const imageUri = photo.path ? photoFileUri(photo.path) : '';

  return <>
    <Pressable onPress={() => setPreviewVisible(true)} style={({pressed}) => [styles.photoTile, pressed && styles.pressed]} accessibilityRole="button" accessibilityLabel={`Abrir foto de ${photo.roomName}, ${photo.clientName}`}>
      {imageUri ? <Image source={{uri: imageUri}} style={styles.photoImage} resizeMode="cover" /> : <View style={styles.photoPlaceholder}><ImageIcon size={24} color={colors.muted} /></View>}
      <View style={styles.photoCaption}><Text numberOfLines={1} style={styles.photoRoom}>{photo.roomName}</Text><Text numberOfLines={1} style={styles.photoClient}>{photo.clientName}</Text><Text numberOfLines={1} style={styles.photoDate}>{formatPhotoCaptureDate(photo.capturedAt || photo.path || photo.createdAt)}</Text></View>
    </Pressable>
    <Modal visible={previewVisible} transparent animationType="fade" statusBarTranslucent onRequestClose={() => setPreviewVisible(false)}>
      <View style={styles.photoViewer}>
        <PhotoPager photos={photos} initialIndex={initialIndex} onClose={() => setPreviewVisible(false)} onOpenRoom={onOpenRoom} onOpenRecord={onOpenRecord} onIdentify={onIdentify} />
      </View>
    </Modal>
  </>;
}

function PhotoPager({photos, initialIndex, onClose, onOpenRoom, onOpenRecord, onIdentify}: {photos: PhotoItem[]; initialIndex: number; onClose: () => void; onOpenRoom?: (photo: PhotoItem) => void; onOpenRecord?: (photo: PhotoItem) => void; onIdentify?: (photo: PhotoItem) => void}) {
  const {width, height} = useWindowDimensions();
  return <FlatList
    data={photos}
    horizontal
    pagingEnabled
    showsHorizontalScrollIndicator={false}
    initialScrollIndex={initialIndex}
    getItemLayout={(_, index) => ({length: width, offset: width * index, index})}
    keyExtractor={(item, index) => `${item.path}-${index}`}
    renderItem={({item}) => <PhotoViewerPage photo={item} width={width} height={height} onClose={onClose} onOpenRoom={onOpenRoom} onOpenRecord={onOpenRecord} onIdentify={onIdentify} />}
  />;
}

function PhotoViewerPage({photo, width, height, onClose, onOpenRoom, onOpenRecord, onIdentify}: {photo: PhotoItem; width: number; height: number; onClose: () => void; onOpenRoom?: (photo: PhotoItem) => void; onOpenRecord?: (photo: PhotoItem) => void; onIdentify?: (photo: PhotoItem) => void}) {
  const [about, setAbout] = useState(false);
  const imageUri = photo.path ? photoFileUri(photo.path) : '';
  const captureDate = formatPhotoCaptureDate(photo.capturedAt || photo.path || photo.createdAt);
  return <View style={[styles.photoViewerPage, {width, height}]}>
    <Pressable style={StyleSheet.absoluteFill} onPress={() => { if (about) setAbout(false); }} accessibilityRole="button" accessibilityLabel="Ocultar informações da foto">
      {imageUri ? <Image pointerEvents="none" source={{uri: imageUri}} style={styles.photoViewerImage} resizeMode="contain" /> : <View pointerEvents="none" style={styles.photoViewerPlaceholder}><ImageIcon size={48} color="#FFFFFF" /></View>}
    </Pressable>
    <Pressable onPress={onClose} style={styles.photoViewerClose} accessibilityRole="button" accessibilityLabel="Fechar foto"><X size={32} color="#FFFFFF" /></Pressable>
    <View style={styles.photoViewerFooter}>
      <View style={styles.photoAboutHeading}><View style={styles.photoViewerTitleRow}><Text numberOfLines={1} style={styles.photoViewerTitle}>{photo.clientName}</Text>
        <Pressable onPress={() => setAbout(value => !value)} style={styles.aboutButton} accessibilityRole="button" accessibilityLabel={about ? 'Recolher informações' : 'Ver informações da foto'}><Text style={styles.aboutButtonText}>{about ? 'Fechar sobre' : 'Sobre'}</Text></Pressable>
      </View><View style={styles.photoViewerMetaRow}><Text numberOfLines={1} style={styles.photoViewerClient}>{photo.roomName}</Text>{captureDate ? <Text style={styles.photoViewerDate}>{captureDate}</Text> : null}</View>
      </View>
      {about ? <View style={styles.photoAboutBody}>
        {photo.modelName ? <Text style={styles.photoAboutText}>Modelo: {photo.modelName}{photo.panelType ? ` · ${photo.panelType === 'SinglePanel' ? 'Painel único' : 'Painel duplo'}` : ''}</Text> : null}
        {photo.fabricMaterialName ? <Text style={styles.photoAboutText}>Tecido: {photo.fabricMaterialName}</Text> : null}
        {photo.rodMaterialName ? <Text style={styles.photoAboutText}>Varão: {photo.rodMaterialName}</Text> : null}
        {photo.widthCentimeters && photo.heightCentimeters ? <Text style={styles.photoAboutText}>Medidas: {photo.widthCentimeters} × {photo.heightCentimeters} cm</Text> : null}
        {!photo.modelName ? <Text style={styles.photoAboutText}>Esta foto ainda não está vinculada a um registro.</Text> : null}
      </View> : null}
      <View style={styles.photoViewerActions}>
        {about && photo.roomId && onOpenRoom ? <Pressable onPress={() => {onClose(); onOpenRoom(photo);}} style={styles.photoViewerAction} accessibilityRole="button"><DoorOpen size={30} color={colors.blueDeep} /><Text style={styles.photoViewerActionText}>Abrir cômodo</Text><ChevronRight size={30} color={colors.blueDeep} /></Pressable> : null}
        {about && photo.roomId && photo.recordId && onOpenRecord ? <Pressable onPress={() => {onClose(); onOpenRecord(photo);}} style={styles.photoViewerAction} accessibilityRole="button"><ClipboardList size={30} color={colors.blueDeep} /><Text style={styles.photoViewerActionText}>Abrir registro</Text><ChevronRight size={30} color={colors.blueDeep} /></Pressable> : null}
        {about && !photo.roomId && onIdentify ? <Pressable onPress={() => {onClose(); setTimeout(() => onIdentify(photo), 250);}} style={styles.photoViewerAction} accessibilityRole="button"><DoorOpen size={30} color={colors.blueDeep} /><Text style={styles.photoViewerActionText}>Identificar cômodo</Text><ChevronRight size={30} color={colors.blueDeep} /></Pressable> : null}
      </View>
    </View>
  </View>;
}

function formatPhotoCaptureDate(value: string): string {
  const fileTimestamp = value.match(/AR_Cortinas_(\d{8})_(\d{6})/);
  const date = fileTimestamp
    ? new Date(Number(fileTimestamp[1].slice(0, 4)), Number(fileTimestamp[1].slice(4, 6)) - 1, Number(fileTimestamp[1].slice(6, 8)), Number(fileTimestamp[2].slice(0, 2)), Number(fileTimestamp[2].slice(2, 4)))
    : new Date(value);
  if (Number.isNaN(date.valueOf())) return '';
  const datePart = [date.getDate(), date.getMonth() + 1, date.getFullYear()].map((part, index) => index < 2 ? String(part).padStart(2, '0') : String(part)).join('/');
  const timePart = `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`;
  return `${datePart} - ${timePart}`;
}

export function CatalogScreen({selectionMode = false}: {selectionMode?: boolean}) {
  const navigation = useNavigation<Nav>();
  const {draft, setDraft, catalogChoice, setCatalogChoice} = useVisitDraft();
  const [category, setCategory] = useState<'Cortinas' | 'Persianas' | 'Acessórios'>(draft?.productCategory === 'Persianas' ? 'Persianas' : catalogChoice.productCategory === 'Persianas' ? 'Persianas' : 'Cortinas');
  const [panelType, setPanelType] = useState<'SinglePanel' | 'DoublePanel'>(draft?.panelType || catalogChoice.panelType);
  const [fabric, setFabric] = useState(draft?.fabricMaterialName || catalogChoice.fabricMaterialName);
  const [rod, setRod] = useState(draft?.rodMaterialName || catalogChoice.rodMaterialName);

  const apply = () => {
    const choice = {productCategory: category, modelName: 'CurtainPrefab', panelType, fabricMaterialName: fabric, fabricMaterialPath: 'Assets/Materials/CurtainFabric.mat', rodMaterialName: rod, rodMaterialPath: 'Assets/Materials/RodMaterial.mat'} as const;
    setCatalogChoice(choice);
    if (draft) setDraft({...draft, ...choice});
    if (selectionMode) navigation.goBack();
    else Alert.alert('Seleção atualizada', 'A seleção estará disponível ao iniciar um registro.');
  };
  const available = category === 'Cortinas';

  return <Page title={selectionMode ? 'Modelo e materiais' : 'Catálogo'} subtitle={selectionMode ? 'Escolha o que será aplicado na visualização em AR' : 'Modelos e materiais pré-definidos'} navigation={selectionMode ? navigation : undefined}>
    <View style={styles.pillRow}>{(['Cortinas', 'Persianas', 'Acessórios'] as const).map(item => <Pill key={item} label={item} selected={category === item} onPress={() => setCategory(item)} />)}</View>
    {!available ? <Card><EmptyState title={`Modelos de ${category.toLocaleLowerCase('pt-BR')} em breve`} description="O catálogo está preparado para receber os modelos pré-definidos. Por enquanto, CurtainPrefab e os materiais padrão estão disponíveis." /></Card> : <>
      <SectionTitle>Modelo</SectionTitle>
      <Pressable onPress={() => setCategory('Cortinas')} style={styles.modelCard}><View style={styles.modelMark}><Sofa size={26} color={colors.blue} /></View><View style={{flex: 1}}><Text style={styles.rowTitle}>Cortina sob medida</Text><Text style={styles.rowDetail}>Modelo 3D: CurtainPrefab · tecido gerado no Unity</Text></View><Check size={22} color={colors.green} /></Pressable>
      <Card><Text style={{fontSize: 13, color: colors.ink, fontWeight: '700'}}>Composição dos painéis</Text><View style={styles.choiceRow}><ChoiceButton label="Painel único" selected={panelType === 'SinglePanel'} onPress={() => setPanelType('SinglePanel')} /><ChoiceButton label="Painel duplo" selected={panelType === 'DoublePanel'} onPress={() => setPanelType('DoublePanel')} /></View></Card>
      <SectionTitle>Materiais disponíveis</SectionTitle>
      <MaterialChoice label="Tecido" detail="CurtainFabric" color="#FFE3BF" selected={fabric === 'CurtainFabric'} onPress={() => setFabric('CurtainFabric')} />
      <MaterialChoice label="Varão e ponteiras" detail="RodMaterial" color="#B6B8BA" selected={rod === 'RodMaterial'} onPress={() => setRod('RodMaterial')} />
    </>}
    {selectionMode ? <Button label="Aplicar seleção" icon={<Check size={22} color="#FFFFFF" />} onPress={apply} disabled={!available} /> : <Button label="Usar no próximo registro" icon={<Check size={22} color="#FFFFFF" />} onPress={apply} disabled={!available} />}
  </Page>;
}

function ChoiceButton({label, selected, onPress}: {label: string; selected: boolean; onPress: () => void}) {
  return <Pressable onPress={onPress} style={[styles.choiceButton, selected && styles.choiceButtonSelected]}><Text style={[styles.choiceText, selected && styles.choiceTextSelected]}>{label}</Text></Pressable>;
}

function MaterialChoice({label, detail, color, selected, onPress}: {label: string; detail: string; color: string; selected: boolean; onPress: () => void}) {
  return <Pressable onPress={onPress} style={[styles.materialCard, selected && styles.materialSelected]}><View style={[styles.swatch, {backgroundColor: color}]} /><View style={{flex: 1}}><Text style={styles.rowTitle}>{label}</Text><Text style={styles.rowDetail}>{detail}</Text></View>{selected ? <Check size={22} color={colors.green} /> : null}</Pressable>;
}

export function VisitScreen() {
  const navigation = useNavigation<Nav>();
  const route = useRoute<any>();
  const {clientId, roomId: initialRoomId} = route.params as {clientId: string; roomId?: string};
  const {clients, addRecord} = useAppState();
  const {draft, setDraft, catalogChoice} = useVisitDraft();
  const client = clients.find(item => item.id === clientId);
  const [roomId, setRoomId] = useState(initialRoomId || '');
  const [step, setStep] = useState(0);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const room = client?.rooms.find(item => item.id === roomId);

  useEffect(() => {
    if (!draft && client && room) setDraft({...createEmptyDraft(client, room), ...catalogChoice});
  }, [draft, client, room, catalogChoice, setDraft]);

  const next = () => {
    setError('');
    if (!room) {setError('Selecione um cômodo para associar este registro.'); return;}
    if (step === 1 && (!(Number(draft?.widthCentimeters) > 0) || !(Number(draft?.heightCentimeters) > 0))) {setError('Informe largura e altura maiores que zero.'); return;}
    if (step < 3) setStep(step + 1);
  };

  const save = async (openAR: boolean) => {
    if (!client || !room || !draft) {setError('Selecione o cliente e o cômodo.'); return;}
    setSaving(true); setError('');
    try {
      const record = await addRecord(client.id, room.id, draft);
      setDraft(null);
      if (openAR) navigation.navigate('UnityAR', {clientId: client.id, roomId: room.id, recordId: record.id});
      else navigation.replace('RoomDetail', {clientId: client.id, roomId: room.id});
    } catch (cause) {setError(cause instanceof Error ? cause.message : 'Não foi possível salvar o registro.');}
    finally {setSaving(false);}
  };

  if (!client) return <Page title="Novo registro" navigation={navigation}><EmptyState title="Cliente não encontrado" description="Volte à lista de clientes e tente novamente." /></Page>;

  return <Page title="Novo registro" subtitle={client.name} navigation={navigation}>
    <View style={styles.stepRow}>{['Visita', 'Medidas', 'Produto', 'Revisão'].map((label, index) => <View key={label} style={styles.stepItem}><View style={[styles.stepDot, index <= step && styles.stepDotActive]}><Text style={[styles.stepNumber, index <= step && styles.stepNumberActive]}>{index < step ? '✓' : index + 1}</Text></View><Text style={[styles.stepLabel, index === step && styles.stepLabelActive]}>{label}</Text></View>)}</View>
    {!room ? <Card><Text style={styles.formTitle}>Em qual cômodo será este registro?</Text><Text style={styles.rowDetail}>Todo registro fica associado a um ambiente do cliente.</Text><View style={{height: 10}} />{client.rooms.length === 0 ? <Button label="Cadastrar um cômodo" icon={<Plus size={22} color="#FFFFFF" />} onPress={() => navigation.navigate('NewRoom', {clientId})} /> : client.rooms.map(item => <LinkRow key={item.id} label={item.name} detail={`${item.records.length} registros`} onPress={() => {setRoomId(item.id); setDraft({...createEmptyDraft(client, item), ...catalogChoice});}} />)}</Card> : null}
    {room && draft ? <>
      {step === 0 ? <Card style={styles.formCard}><Text style={styles.formTitle}>Anotações da visita</Text><View style={styles.lockedRoom}><DoorOpen size={22} color={colors.blue} /><Text style={styles.lockedRoomText}>{room.name}</Text></View><TextField label="Observações" value={draft.visitNotes} onChangeText={value => setDraft({...draft, visitNotes: value})} placeholder="O que o cliente precisa para este ambiente?" multiline /></Card> : null}
      {step === 1 ? <Card style={styles.formCard}><Text style={styles.formTitle}>Cômodo e medidas</Text><TextField label="Abertura / janela" value={draft.openingName} onChangeText={value => setDraft({...draft, openingName: value})} placeholder="Ex.: Janela principal" /><View style={styles.measureRow}><View style={{flex: 1}}><TextField label="Largura (cm)" value={draft.widthCentimeters ? String(draft.widthCentimeters) : ''} onChangeText={value => setDraft({...draft, widthCentimeters: Number(value.replace(',', '.')) || 0})} keyboardType="decimal-pad" placeholder="Ex.: 180" required /></View><View style={{flex: 1}}><TextField label="Altura (cm)" value={draft.heightCentimeters ? String(draft.heightCentimeters) : ''} onChangeText={value => setDraft({...draft, heightCentimeters: Number(value.replace(',', '.')) || 0})} keyboardType="decimal-pad" placeholder="Ex.: 140" required /></View></View><TextField label="Observações do cômodo" value={draft.roomNotes} onChangeText={value => setDraft({...draft, roomNotes: value})} placeholder="Instalação, acabamento, detalhes…" multiline /></Card> : null}
      {step === 2 ? <Card style={styles.formCard}><Text style={styles.formTitle}>Produto e materiais</Text><Text style={styles.rowDetail}>Escolha o modelo de cortina e os materiais para visualizar com as medidas do cômodo.</Text><View style={styles.selectionSummary}><Sofa size={22} color={colors.blue} /><View style={{flex: 1}}><Text style={styles.rowTitle}>{draft.modelName}</Text><Text style={styles.rowDetail}>{draft.panelType === 'SinglePanel' ? 'Painel único' : 'Painel duplo'}</Text><Text style={styles.rowDetail}>Tecido: {draft.fabricMaterialName}</Text><Text style={styles.rowDetail}>Varão: {draft.rodMaterialName}</Text></View></View><Button label="Abrir modelos e materiais" variant="secondary" icon={<Sparkles size={22} color={colors.blue} />} onPress={() => navigation.navigate('CatalogSelection')} /></Card> : null}
      {step === 3 ? <Card style={styles.formCard}><Text style={styles.formTitle}>Confira o registro</Text><SummaryRow label="Cliente" value={client.name} /><SummaryRow label="Cômodo" value={room.name} /><SummaryRow label="Abertura" value={draft.openingName || 'Não informada'} /><SummaryRow label="Medidas" value={`${draft.widthCentimeters} × ${draft.heightCentimeters} cm`} /><SummaryRow label="Modelo" value={`${draft.modelName} · ${draft.panelType === 'SinglePanel' ? 'Painel único' : 'Painel duplo'}`} /><SummaryRow label="Materiais" value={`${draft.fabricMaterialName} · ${draft.rodMaterialName}`} /><SummaryRow label="Observações" value={draft.visitNotes || 'Nenhuma'} /></Card> : null}
      {error ? <Text style={styles.errorText}>{error}</Text> : null}
      <View style={styles.wizardActions}>{step > 0 ? <View style={{flex: 1}}><Button label="Voltar etapa" variant="secondary" onPress={() => setStep(step - 1)} /></View> : <View style={{flex: 1}}><Button label="Cancelar" variant="quiet" onPress={() => navigation.goBack()} /></View>}
        {step < 3 ? <View style={{flex: 1}}><Button label={step === 2 ? 'Revisar' : 'Continuar'} icon={<ArrowRight size={22} color="#FFFFFF" />} onPress={next} /></View> : <View style={{flex: 1}}><Button label="Salvar registro" icon={<Check size={22} color="#FFFFFF" />} onPress={() => save(false)} loading={saving} /></View>}
      </View>
      {step === 3 ? <Button label="Salvar e visualizar em AR" variant="secondary" icon={<Camera size={22} color={colors.blue} />} onPress={() => save(true)} loading={saving} /> : null}
    </> : null}
  </Page>;
}

function SummaryRow({label, value}: {label: string; value: string}) {
  return <View style={styles.summaryRow}><Text style={styles.summaryLabel}>{label}</Text><Text style={styles.summaryValue}>{value}</Text></View>;
}

export function UnityARScreen() {
  const navigation = useNavigation<Nav>();
  const route = useRoute<any>();
  const params = route.params as {clientId: string; roomId?: string; recordId?: string};
  const photoOnly = !params.roomId || !params.recordId;
  const {clients, reload} = useAppState();
  const [opening, setOpening] = useState(false);
  const [error, setError] = useState('');
  useFocusEffect(useCallback(() => { void reload(); }, [reload]));
  useEffect(() => {
    const subscription = AppState.addEventListener('change', state => {
      if (state === 'active') void reload();
    });
    return () => subscription.remove();
  }, [reload]);
  const client = clients.find(item => item.id === params.clientId);
  const room = client?.rooms.find(item => item.id === params.roomId);
  const record = room?.records.find(item => item.id === params.recordId);

  const openUnity = async () => {
    setError('');
    if (Platform.OS !== 'android') {
      setError('A experiência AR integrada está disponível apenas no aplicativo Android neste momento.');
      return;
    }
    if (!NativeModules.UnityArModule?.openAR) {
      setError('Este APK não inclui o módulo de integração com o Unity. Gere e instale novamente o aplicativo Android pelo projeto mobile.');
      return;
    }
    setOpening(true);
    try {
      await NativeModules.UnityArModule.openAR({
        clientId: params.clientId,
        roomId: params.roomId || '',
        recordId: params.recordId || '',
        draftJson: JSON.stringify({
          clientName: client?.name || '', phone: client?.phone || '', address: client?.address || '',
          visitNotes: record?.notes || '', roomName: room?.name || '', openingName: record?.openingName || '',
          widthCentimeters: record?.widthCentimeters || 0, heightCentimeters: record?.heightCentimeters || 0,
          roomNotes: record?.roomNotes || '', productCategory: record?.productCategory || 'Cortinas',
          modelName: record?.modelName || 'CurtainPrefab', panelType: record?.panelType || 'DoublePanel',
          fabricMaterialName: record?.fabricMaterialName || 'CurtainFabric',
          fabricMaterialPath: 'Assets/Materials/CurtainFabric.mat', rodMaterialName: record?.rodMaterialName || 'RodMaterial',
          rodMaterialPath: 'Assets/Materials/RodMaterial.mat',
        }),
      });
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível abrir a cena AR.');
    } finally {setOpening(false);}
  };

  return <Page title={photoOnly ? 'Foto do cliente' : 'Visualização em AR'} subtitle={photoOnly ? (client?.name || 'Cliente') : `${client?.name || 'Cliente'} · ${room?.name || 'Cômodo'}`} navigation={navigation}>
    <View style={styles.arStage}><View style={styles.arRoom}><View style={styles.arWall} /><View style={styles.arFloor} /><View style={styles.arWindow}><View style={styles.arCurtainLeft} /><View style={styles.arCurtainRight} /><View style={styles.arRod} /></View></View><View style={styles.arBadge}><Sparkles size={14} color={colors.gold} /><Text style={styles.arBadgeText}>ESPAÇO PRONTO PARA AR</Text></View></View>
    <Card style={styles.formCard}><Text style={styles.formTitle}>{photoOnly ? 'Registrar foto sem cômodo' : (record?.modelName || 'Cortina sob medida')}</Text>
      {photoOnly ? <Text style={styles.rowDetail}>A imagem ficará no perfil de {client?.name || 'cliente'} e poderá ser vinculada a um cômodo depois.</Text> : <><Text style={styles.rowDetail}>{record?.openingName || 'Abertura'} · {record?.widthCentimeters || '—'} × {record?.heightCentimeters || '—'} cm</Text><Text style={styles.rowDetail}>Tecido: {record?.fabricMaterialName || 'CurtainFabric'} · Varão: {record?.rodMaterialName || 'RodMaterial'}</Text></>}
      <Button label={photoOnly ? 'Abrir câmera para tirar foto' : 'Abrir câmera AR do Unity'} icon={<Camera size={22} color="#FFFFFF" />} onPress={openUnity} loading={opening} />
      {error ? <Text style={styles.errorText}>{error}</Text> : <Text style={styles.helpText}>A câmera e a captura continuarão sendo executadas pelo Unity.</Text>}
      <Button label={photoOnly ? 'Voltar ao perfil' : 'Voltar ao cômodo'} variant="secondary" icon={<DoorOpen size={22} color={colors.blue} />} onPress={() => photoOnly ? navigation.replace('ClientProfile', {clientId: params.clientId}) : navigation.replace('RoomDetail', {clientId: params.clientId, roomId: params.roomId!})} />
    </Card>
  </Page>;
}

export function TabIcon({name, color, size}: {name: keyof MainTabParamList; color: string; size: number}) {
  const iconSize = size + 4;
  switch (name) {
    case 'Início': return <Sofa size={iconSize} color={color} />;
    case 'Clientes': return <Users size={iconSize} color={color} />;
    case 'Fotos': return <Images size={iconSize} color={color} />;
    case 'Catálogo': return <Sparkles size={iconSize} color={color} />;
  }
}

const styles = StyleSheet.create({
  pressed: {opacity: 0.76, transform: [{scale: 0.995}]},
  welcomeRow: {flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingTop: 8, paddingBottom: 2},
  eyebrow: {fontSize: 10, fontWeight: '800', letterSpacing: 1.2, color: colors.blue, marginBottom: 7},
  welcomeTitle: {fontSize: 24, lineHeight: 30, fontWeight: '800', color: colors.ink, letterSpacing: -0.5},
  welcomeIcon: {width: 54, height: 54, borderRadius: 18, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  hero: {borderRadius: radius.large, padding: 20, backgroundColor: colors.blue, gap: 12, overflow: 'hidden'},
  heroTop: {flexDirection: 'row', alignItems: 'center', gap: 9},
  heroIcon: {width: 32, height: 32, borderRadius: 12, backgroundColor: 'rgba(255,255,255,0.13)', alignItems: 'center', justifyContent: 'center'},
  heroTag: {fontSize: 10, fontWeight: '800', letterSpacing: 1.1, color: '#D9E9F1'},
  heroTitle: {fontSize: 21, lineHeight: 27, fontWeight: '800', color: '#FFFFFF'},
  heroAction: {height: 39, alignSelf: 'flex-start', borderRadius: 12, paddingHorizontal: 13, backgroundColor: 'rgba(255,255,255,0.15)', flexDirection: 'row', alignItems: 'center', gap: 8},
  heroActionText: {color: '#FFFFFF', fontSize: 13, fontWeight: '800'},
  statRow: {flexDirection: 'row', gap: 9},
  statCard: {flex: 1, minHeight: 105, padding: 12, gap: 4},
  statIcon: {width: 30, height: 30, borderRadius: 10, alignItems: 'center', justifyContent: 'center', marginBottom: 3},
  statValue: {fontSize: 22, fontWeight: '800', color: colors.ink},
  statLabel: {fontSize: 11, color: colors.muted},
  seeAll: {fontSize: 12, color: colors.blue, fontWeight: '800'},
  actionCard: {paddingVertical: 4},
  divider: {height: 1, backgroundColor: colors.line},
  recentCard: {backgroundColor: colors.surface, borderRadius: 16, borderWidth: 1, borderColor: colors.line, padding: 13, flexDirection: 'row', alignItems: 'center', gap: 12},
  recentIcon: {width: 40, height: 40, borderRadius: 14, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  recentCopy: {flex: 1, gap: 4},
  rowTitle: {fontSize: 14, fontWeight: '800', color: colors.ink},
  rowDetail: {fontSize: 12, lineHeight: 17, color: colors.muted},
  muted: {fontSize: 13, lineHeight: 20, color: colors.muted},
  addHeaderButton: {width: 42, height: 42, borderRadius: 14, backgroundColor: colors.blue, alignItems: 'center', justifyContent: 'center'},
  listContent: {paddingHorizontal: 20, paddingTop: 3, paddingBottom: 26},
  listHeader: {gap: 10, paddingBottom: 14},
  searchBox: {height: 48, borderRadius: 14, borderWidth: 1, borderColor: colors.line, backgroundColor: colors.surface, paddingHorizontal: 13, flexDirection: 'row', alignItems: 'center', gap: 9},
  searchInput: {flex: 1, paddingVertical: 0, fontSize: 13, color: colors.ink},
  listHint: {fontSize: 12, color: colors.muted},
  clientCard: {paddingHorizontal: 13, paddingVertical: 0},
  clientOpen: {flexDirection: 'row', alignItems: 'center', gap: 11, paddingTop: 13, paddingBottom: 10},
  clientCopy: {flex: 1, gap: 3},
  avatar: {width: 43, height: 43, borderRadius: 15, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  avatarLarge: {width: 50, height: 50, borderRadius: 17, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  clientCardFooter: {borderTopWidth: 1, borderTopColor: colors.line, minHeight: 43, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 8},
  addressLine: {flex: 1, flexDirection: 'row', alignItems: 'center', gap: 5},
  addressText: {fontSize: 11, color: colors.muted, flex: 1},
  moreButton: {width: 38, height: 36, alignItems: 'center', justifyContent: 'center', borderRadius: 12},
  modalBackdrop: {flex: 1, backgroundColor: 'rgba(14,25,32,0.35)', justifyContent: 'flex-end'},
  actionSheet: {backgroundColor: colors.surface, paddingHorizontal: 21, paddingTop: 11, paddingBottom: 30, borderTopLeftRadius: 24, borderTopRightRadius: 24},
  sheetHandle: {width: 38, height: 4, borderRadius: 3, backgroundColor: '#D3DADD', alignSelf: 'center', marginBottom: 15},
  sheetTitleRow: {flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4},
  sheetTitle: {fontSize: 18, fontWeight: '800', color: colors.ink, marginBottom: 7},
  formCard: {gap: 16},
  formIntro: {flexDirection: 'row', alignItems: 'center', gap: 12},
  errorText: {fontSize: 13, color: colors.danger, lineHeight: 18, fontWeight: '600'},
  profileCard: {gap: 13},
  profileHead: {flexDirection: 'row', alignItems: 'center', gap: 12},
  profileName: {fontSize: 18, fontWeight: '800', color: colors.ink, marginBottom: 4},
  editIconButton: {width: 40, height: 40, borderRadius: 13, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  profileAddress: {flexDirection: 'row', gap: 7, alignItems: 'center'},
  profileStats: {borderTopColor: colors.line, borderTopWidth: 1, paddingTop: 12, flexDirection: 'row', alignItems: 'center', gap: 15},
  profileStat: {fontSize: 12, color: colors.muted},
  profileStatNum: {fontSize: 15, fontWeight: '800', color: colors.ink},
  verticalDivider: {width: 1, height: 18, backgroundColor: colors.line},
  segmented: {padding: 4, borderRadius: 14, backgroundColor: '#E9EEF0', flexDirection: 'row'},
  segment: {flex: 1, paddingVertical: 10, alignItems: 'center', borderRadius: 11},
  segmentActive: {backgroundColor: colors.surface},
  segmentText: {fontSize: 13, color: colors.muted, fontWeight: '700'},
  segmentTextActive: {color: colors.blue, fontWeight: '800'},
  inlineAdd: {flexDirection: 'row', alignItems: 'center', gap: 3},
  roomCard: {minHeight: 69, backgroundColor: colors.surface, borderRadius: 17, borderWidth: 1, borderColor: colors.line, paddingHorizontal: 13, flexDirection: 'row', alignItems: 'center', gap: 12},
  roomIcon: {width: 43, height: 43, borderRadius: 14, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  formTitle: {fontSize: 17, color: colors.ink, fontWeight: '800'},
  photoList: {paddingHorizontal: 20, paddingTop: 5, paddingBottom: 28, gap: 10},
  photoRow: {gap: 10},
  photoGrid: {flexDirection: 'row', flexWrap: 'wrap', gap: 10},
  photoGridCell: {width: '48%'},
  photoTile: {backgroundColor: colors.surface, borderRadius: 15, borderWidth: 1, borderColor: colors.line, overflow: 'hidden', flex: 1},
  photoImage: {width: '100%', height: 128, backgroundColor: '#E7ECEF'},
  photoPlaceholder: {width: '100%', height: 128, alignItems: 'center', justifyContent: 'center', backgroundColor: '#E7ECEF'},
  photoCaption: {padding: 9, gap: 3},
  photoRoom: {fontSize: 12, fontWeight: '800', color: colors.ink},
  photoClient: {fontSize: 10, color: colors.muted},
  photoDate: {fontSize: 10, color: colors.muted, marginTop: 2},
  photoViewer: {flex: 1, backgroundColor: 'rgba(7,15,20,0.98)', justifyContent: 'center'},
  photoViewerPage: {flex: 1, backgroundColor: 'rgba(7,15,20,0.98)', justifyContent: 'flex-end'},
  photoViewerClose: {position: 'absolute', zIndex: 1, top: 48, right: 18, width: 52, height: 52, borderRadius: 16, backgroundColor: 'rgba(255,255,255,0.16)', alignItems: 'center', justifyContent: 'center'},
  photoViewerImage: {width: '100%', flex: 1},
  photoViewerPlaceholder: {flex: 1, alignItems: 'center', justifyContent: 'center'},
  photoViewerFooter: {paddingHorizontal: 22, paddingTop: 14, paddingBottom: 26, gap: 9, backgroundColor: 'rgba(7,15,20,0.73)'},
  photoAboutHeading: {gap: 4},
  photoViewerTitleRow: {flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 9},
  photoViewerMetaRow: {flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 10},
  aboutButton: {paddingHorizontal: 10, paddingVertical: 5, borderRadius: 16, backgroundColor: 'rgba(255,255,255,0.16)'},
  aboutButtonText: {fontSize: 11, color: '#FFFFFF', fontWeight: '800'},
  photoAboutBody: {paddingVertical: 5, gap: 5},
  photoAboutText: {fontSize: 13, lineHeight: 18, color: '#E1E8EB'},
  photoViewerActions: {gap: 7},
  photoViewerTitle: {flexShrink: 1, fontSize: 18, color: '#FFFFFF', fontWeight: '800'},
  photoViewerClient: {flex: 1, fontSize: 13, color: '#C7D0D5'},
  photoViewerDate: {fontSize: 12, color: '#C7D0D5', flexShrink: 0},
  photoViewerAction: {marginTop: 12, minHeight: 48, borderRadius: 15, paddingHorizontal: 15, backgroundColor: '#E7F1F5', flexDirection: 'row', alignItems: 'center', gap: 9},
  photoViewerActionText: {flex: 1, fontSize: 14, color: colors.blueDeep, fontWeight: '800'},
  emptyFill: {flex: 1, justifyContent: 'center', paddingHorizontal: 20},
  roomSummary: {flexDirection: 'row', alignItems: 'center', gap: 11, paddingVertical: 6},
  countBadge: {minWidth: 25, textAlign: 'center', overflow: 'hidden', borderRadius: 8, color: colors.blue, backgroundColor: colors.bluePale, fontSize: 12, fontWeight: '800', paddingHorizontal: 7, paddingVertical: 3},
  recordCard: {backgroundColor: colors.surface, borderRadius: 17, borderWidth: 1, borderColor: colors.line, padding: 13, flexDirection: 'row', alignItems: 'flex-start', gap: 11},
  recordIcon: {width: 39, height: 39, borderRadius: 13, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  recordTop: {flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 8},
  recordPhotoCount: {fontSize: 10, color: colors.blue, fontWeight: '800'},
  recordNotes: {fontSize: 12, lineHeight: 17, color: colors.ink, marginTop: 2},
  formCardTitle: {fontSize: 17, fontWeight: '800', color: colors.ink},
  pillRow: {flexDirection: 'row', gap: 8, flexWrap: 'wrap'},
  modelCard: {padding: 14, flexDirection: 'row', alignItems: 'center', gap: 12, borderRadius: 17, borderWidth: 1, borderColor: '#C7DBE4', backgroundColor: colors.bluePale},
  modelMark: {width: 48, height: 48, borderRadius: 16, backgroundColor: colors.surface, alignItems: 'center', justifyContent: 'center'},
  choiceRow: {flexDirection: 'row', gap: 9, marginTop: 10},
  choiceButton: {flex: 1, minHeight: 46, borderWidth: 1, borderColor: colors.line, backgroundColor: colors.background, borderRadius: 13, alignItems: 'center', justifyContent: 'center', paddingHorizontal: 6},
  choiceButtonSelected: {borderColor: colors.blue, backgroundColor: colors.bluePale},
  choiceText: {fontSize: 12, color: colors.muted, fontWeight: '700'},
  choiceTextSelected: {color: colors.blueDeep},
  materialCard: {minHeight: 68, borderRadius: 16, borderWidth: 1, borderColor: colors.line, backgroundColor: colors.surface, flexDirection: 'row', alignItems: 'center', gap: 12, padding: 12},
  materialSelected: {borderColor: colors.blue, backgroundColor: '#F0F6F8'},
  swatch: {width: 40, height: 40, borderRadius: 13, borderWidth: 1, borderColor: 'rgba(20,40,50,0.08)'},
  stepRow: {flexDirection: 'row', justifyContent: 'space-between', paddingVertical: 8},
  stepItem: {alignItems: 'center', gap: 6, flex: 1},
  stepDot: {width: 27, height: 27, borderRadius: 10, borderWidth: 1, borderColor: colors.line, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.surface},
  stepDotActive: {backgroundColor: colors.blue, borderColor: colors.blue},
  stepNumber: {fontSize: 11, color: colors.muted, fontWeight: '800'},
  stepNumberActive: {color: '#FFFFFF'},
  stepLabel: {fontSize: 9, color: colors.muted},
  stepLabelActive: {fontWeight: '800', color: colors.blue},
  lockedRoom: {flexDirection: 'row', alignItems: 'center', gap: 7, padding: 11, backgroundColor: colors.bluePale, borderRadius: 12},
  lockedRoomText: {fontSize: 13, fontWeight: '800', color: colors.blueDeep},
  measureRow: {flexDirection: 'row', gap: 10},
  selectionSummary: {flexDirection: 'row', gap: 11, padding: 13, borderRadius: 15, backgroundColor: colors.background},
  wizardActions: {flexDirection: 'row', gap: 10},
  summaryRow: {borderBottomWidth: 1, borderBottomColor: colors.line, paddingVertical: 9, gap: 4},
  summaryLabel: {fontSize: 11, color: colors.muted, fontWeight: '700'},
  summaryValue: {fontSize: 14, color: colors.ink, fontWeight: '700'},
  editableRecordHeader: {flexDirection: 'row', alignItems: 'center', gap: 8},
  editableRecordValue: {flex: 1, gap: 4},
  editableRecordInput: {flex: 1},
  editableRecordActions: {flexDirection: 'row', alignItems: 'center', gap: 5},
  editableRecordIcon: {width: 38, height: 38, borderRadius: 13, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  arStage: {height: 240, borderRadius: radius.large, overflow: 'hidden', backgroundColor: '#EAF0F2', padding: 15, justifyContent: 'flex-end'},
  arRoom: {flex: 1, borderRadius: 15, overflow: 'hidden', backgroundColor: '#DFE8EC', position: 'relative'},
  arWall: {position: 'absolute', top: 0, left: 0, right: 0, height: '75%', backgroundColor: '#E9EEF0'},
  arFloor: {position: 'absolute', bottom: 0, left: 0, right: 0, height: '28%', backgroundColor: '#C8D5DB', transform: [{skewY: '-5deg'}]},
  arWindow: {position: 'absolute', top: 35, left: '26%', width: '48%', height: 108, borderRadius: 3, backgroundColor: '#F8FCFD', borderWidth: 5, borderColor: '#9BAEB7'},
  arCurtainLeft: {position: 'absolute', top: 0, bottom: 0, left: -7, width: '23%', backgroundColor: '#DAB98A'},
  arCurtainRight: {position: 'absolute', top: 0, bottom: 0, right: -7, width: '23%', backgroundColor: '#DAB98A'},
  arRod: {position: 'absolute', top: -8, left: -11, right: -11, height: 7, borderRadius: 5, backgroundColor: '#697985'},
  arBadge: {position: 'absolute', left: 27, bottom: 26, backgroundColor: colors.blue, borderRadius: radius.pill, paddingHorizontal: 10, paddingVertical: 7, flexDirection: 'row', alignItems: 'center', gap: 6},
  arBadgeText: {fontSize: 9, letterSpacing: 0.5, fontWeight: '800', color: '#FFFFFF'},
  helpText: {fontSize: 12, lineHeight: 18, color: colors.muted},
  profileMeta: {fontSize: 12, color: colors.muted},
});
