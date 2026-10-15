import React, {useEffect, useMemo, useState} from 'react';
import {ActivityIndicator, Alert, Dimensions, FlatList, Linking, Modal, Pressable, StatusBar, StyleSheet, Text, TextInput, View} from 'react-native';
import {createNavigationContainerRef, NavigationContainer} from '@react-navigation/native';
import {createBottomTabNavigator} from '@react-navigation/bottom-tabs';
import {createNativeStackNavigator} from '@react-navigation/native-stack';
import {initialWindowMetrics, SafeAreaProvider, useSafeAreaInsets} from 'react-native-safe-area-context';
import {Camera, ChevronRight, Search, UserPlus, X} from 'lucide-react-native';
import {
  CatalogScreen,
  ClientFormScreen,
  ClientProfileScreen,
  ClientsScreen,
  HomeScreen,
  NewRoomScreen,
  PhotosScreen,
  RoomDetailScreen,
  RecordDetailScreen,
  TabIcon,
  UnityARScreen,
  VisitScreen,
} from './src/screens';
import {AppStateProvider, DraftProvider} from './src/state/AppState';
import {colors} from './src/theme';
import {openQuickAR} from './src/quickAR';
import {useAppState} from './src/state/AppState';
import type {MainTabParamList, RootStackParamList} from './src/types';

const Stack = createNativeStackNavigator<RootStackParamList>();
const Tabs = createBottomTabNavigator<MainTabParamList>();
const navigationRef = createNavigationContainerRef<RootStackParamList>();
let pendingQuickARClientId: string | null = null;

function navigateToQuickARClient(url: string | null) {
  const match = url ? /^decorit:\/\/client\/(.+)$/.exec(url) : null;
  if (!match) return;
  const clientId = decodeURIComponent(match[1]);
  if (navigationRef.isReady()) navigationRef.navigate('ClientProfile', {clientId});
  else pendingQuickARClientId = clientId;
}

function MainTabs() {
  const insets = useSafeAreaInsets();
  const {clients, addClient} = useAppState();
  const [pickerVisible, setPickerVisible] = useState(false);
  const [clientQuery, setClientQuery] = useState('');
  const [launching, setLaunching] = useState(false);
  const matches = useMemo(() => {
    const query = clientQuery.trim().toLocaleLowerCase('pt-BR');
    return query ? clients.filter(client => client.name.toLocaleLowerCase('pt-BR').includes(query)) : clients;
  }, [clients, clientQuery]);

  const launch = async (client: (typeof clients)[number]) => {
    setLaunching(true);
    try {
      await openQuickAR(client);
      setPickerVisible(false);
      setClientQuery('');
    } catch (cause) {
      Alert.alert('Não foi possível abrir a câmera AR', cause instanceof Error ? cause.message : 'Tente novamente.');
    } finally {setLaunching(false);}
  };

  const createAndLaunch = async () => {
    const name = clientQuery.trim();
    if (!name || matches.length) return;
    try {
      const client = await addClient(name);
      await launch(client);
    } catch (cause) {
      Alert.alert('Não foi possível criar o cliente', cause instanceof Error ? cause.message : 'Tente novamente.');
    }
  };

  return (
    <View style={{flex: 1}}>
    <Tabs.Navigator
      screenOptions={({route}) => ({
        headerShown: false,
        tabBarActiveTintColor: colors.blue,
        tabBarInactiveTintColor: colors.muted,
        tabBarStyle: {
          backgroundColor: '#FFFFFF',
          borderTopColor: colors.line,
          height: 72 + insets.bottom,
          paddingTop: 7,
          paddingBottom: 7 + insets.bottom,
        },
        tabBarLabelStyle: {fontSize: 11, fontWeight: '600'},
        tabBarIcon: ({color, size}) => route.name === 'AR Rápido' ? null : <TabIcon name={route.name} color={color} size={size} />,
        tabBarButton: route.name === 'AR Rápido' ? props => <Pressable {...props} onPress={() => {setClientQuery(''); setPickerVisible(true);}} style={[props.style, appStyles.quickCameraTab]} accessibilityRole="button" accessibilityLabel="Abrir câmera AR">
          <View style={appStyles.quickCameraCircle}><Camera size={29} color="#FFFFFF" /></View><Text style={appStyles.quickCameraLabel}>AR</Text>
        </Pressable> : undefined,
      })}>
      <Tabs.Screen name="Início" component={HomeScreen} />
      <Tabs.Screen name="Clientes" component={ClientsScreen} />
      <Tabs.Screen name="AR Rápido" component={QuickARPlaceholder} options={{tabBarLabel: ''}} />
      <Tabs.Screen name="Fotos" component={PhotosScreen} />
      <Tabs.Screen name="Catálogo" component={CatalogScreen} />
    </Tabs.Navigator>
    <Modal visible={pickerVisible} transparent animationType="slide" onRequestClose={() => setPickerVisible(false)}>
      <Pressable style={appStyles.quickModalBackdrop} onPress={() => setPickerVisible(false)}>
        <Pressable style={appStyles.quickModalCard} onPress={event => event.stopPropagation()}>
          <View style={appStyles.quickModalHeading}><View style={{flex: 1}}><Text style={appStyles.quickModalTitle}>Visualizar em AR</Text><Text style={appStyles.quickModalHint}>Escolha o cliente para salvar as fotos.</Text></View><Pressable onPress={() => setPickerVisible(false)} style={appStyles.quickModalClose} accessibilityRole="button" accessibilityLabel="Fechar"><X size={22} color={colors.muted} /></Pressable></View>
          <View style={appStyles.quickSearch}><Search size={21} color={colors.muted} /><TextInput value={clientQuery} onChangeText={setClientQuery} placeholder="Buscar cliente pelo nome" placeholderTextColor="#99A5AC" style={appStyles.quickSearchInput} autoCapitalize="words" autoFocus /></View>
          {matches.length ? <FlatList data={matches} keyExtractor={client => client.id} keyboardShouldPersistTaps="handled" style={appStyles.quickClientList} renderItem={({item}) => <Pressable onPress={() => {void launch(item);}} disabled={launching} style={appStyles.quickClientRow}><View style={appStyles.quickClientAvatar}><Text style={appStyles.quickClientInitial}>{item.name.trim().charAt(0).toLocaleUpperCase('pt-BR')}</Text></View><View style={{flex: 1}}><Text style={appStyles.quickClientName}>{item.name}</Text>{item.phone ? <Text style={appStyles.quickClientPhone}>{item.phone}</Text> : null}</View>{launching ? <ActivityIndicator color={colors.blue} /> : <ChevronRight size={22} color={colors.muted} />}</Pressable>} /> : <View style={appStyles.quickNoResults}><Text style={appStyles.quickNoResultsTitle}>{clientQuery.trim() ? 'Nenhum cliente encontrado com esse nome.' : 'Ainda não há clientes cadastrados.'}</Text>{clientQuery.trim() ? <Pressable onPress={() => {void createAndLaunch();}} disabled={launching} style={appStyles.quickCreateButton}>{launching ? <ActivityIndicator color="#FFFFFF" /> : <UserPlus size={21} color="#FFFFFF" />}<Text style={appStyles.quickCreateText}>Criar “{clientQuery.trim()}” e abrir a câmera</Text></Pressable> : null}</View>}
        </Pressable>
      </Pressable>
    </Modal>
    </View>
  );
}

function QuickARPlaceholder() { return null; }

export default function App() {
  useEffect(() => {
    const subscription = Linking.addEventListener('url', event => navigateToQuickARClient(event.url));
    void Linking.getInitialURL().then(url => navigateToQuickARClient(url ?? null));
    return () => subscription.remove();
  }, []);

  return (
    <SafeAreaProvider initialMetrics={initialWindowMetrics ?? {frame: {x: 0, y: 0, width: Dimensions.get('window').width, height: Dimensions.get('window').height}, insets: {top: 0, right: 0, bottom: 0, left: 0}}}>
      <StatusBar barStyle="dark-content" />
      <AppStateProvider>
        <DraftProvider>
          <NavigationContainer ref={navigationRef} onReady={() => {
            if (pendingQuickARClientId) {
              navigationRef.navigate('ClientProfile', {clientId: pendingQuickARClientId});
              pendingQuickARClientId = null;
            }
          }}>
            <Stack.Navigator screenOptions={{headerShown: false, contentStyle: {backgroundColor: colors.background}}}>
              <Stack.Screen name="MainTabs" component={MainTabs} />
              <Stack.Screen name="ClientProfile" component={ClientProfileScreen} />
              <Stack.Screen name="ClientForm" component={ClientFormScreen} />
              <Stack.Screen name="NewRoom" component={NewRoomScreen} />
              <Stack.Screen name="RoomDetail" component={RoomDetailScreen} />
              <Stack.Screen name="RecordDetail" component={RecordDetailScreen} />
              <Stack.Screen name="NewRecord" component={VisitScreen} />
              <Stack.Screen name="CatalogSelection">
                {() => <CatalogScreen selectionMode />}
              </Stack.Screen>
              <Stack.Screen name="UnityAR" component={UnityARScreen} />
            </Stack.Navigator>
          </NavigationContainer>
        </DraftProvider>
      </AppStateProvider>
    </SafeAreaProvider>
  );
}

const appStyles = StyleSheet.create({
  quickCameraTab: {flex: 1, alignItems: 'center', justifyContent: 'center', marginTop: -13, gap: 2},
  quickCameraCircle: {width: 55, height: 55, borderRadius: 20, backgroundColor: colors.blue, borderWidth: 3, borderColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center', elevation: 7, shadowColor: colors.shadow, shadowOffset: {width: 0, height: 3}, shadowOpacity: 0.24, shadowRadius: 5},
  quickCameraLabel: {fontSize: 10, fontWeight: '800', color: colors.blueDeep},
  quickModalBackdrop: {flex: 1, justifyContent: 'center', alignItems: 'center', padding: 22, backgroundColor: 'rgba(13,24,31,0.50)'},
  quickModalCard: {width: '100%', maxHeight: '78%', paddingHorizontal: 20, paddingTop: 19, paddingBottom: 22, borderRadius: 26, backgroundColor: '#FFFFFF', gap: 14, elevation: 12},
  quickModalHeading: {flexDirection: 'row', alignItems: 'center', gap: 12},
  quickModalTitle: {fontSize: 20, color: colors.ink, fontWeight: '800'},
  quickModalHint: {fontSize: 13, color: colors.muted, marginTop: 3},
  quickModalClose: {width: 42, height: 42, borderRadius: 14, backgroundColor: colors.background, alignItems: 'center', justifyContent: 'center'},
  quickSearch: {height: 50, borderRadius: 15, borderWidth: 1, borderColor: colors.line, backgroundColor: colors.background, flexDirection: 'row', alignItems: 'center', gap: 10, paddingHorizontal: 14},
  quickSearchInput: {flex: 1, color: colors.ink, fontSize: 15, paddingVertical: 0},
  quickClientList: {flexGrow: 0, maxHeight: 360},
  quickClientRow: {minHeight: 62, flexDirection: 'row', alignItems: 'center', gap: 12, borderBottomWidth: 1, borderBottomColor: colors.line},
  quickClientAvatar: {width: 40, height: 40, borderRadius: 14, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  quickClientInitial: {fontSize: 16, color: colors.blue, fontWeight: '800'},
  quickClientName: {fontSize: 15, color: colors.ink, fontWeight: '700'},
  quickClientPhone: {fontSize: 12, color: colors.muted, marginTop: 2},
  quickNoResults: {gap: 12, paddingVertical: 10},
  quickNoResultsTitle: {fontSize: 14, color: colors.muted},
  quickCreateButton: {minHeight: 50, borderRadius: 15, paddingHorizontal: 15, backgroundColor: colors.blue, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 9},
  quickCreateText: {fontSize: 14, fontWeight: '800', color: '#FFFFFF'},
});

