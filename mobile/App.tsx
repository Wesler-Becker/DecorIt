import React from 'react';
import {Dimensions, StatusBar} from 'react-native';
import {NavigationContainer} from '@react-navigation/native';
import {createBottomTabNavigator} from '@react-navigation/bottom-tabs';
import {createNativeStackNavigator} from '@react-navigation/native-stack';
import {initialWindowMetrics, SafeAreaProvider, useSafeAreaInsets} from 'react-native-safe-area-context';
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
import type {MainTabParamList, RootStackParamList} from './src/types';

const Stack = createNativeStackNavigator<RootStackParamList>();
const Tabs = createBottomTabNavigator<MainTabParamList>();

function MainTabs() {
  const insets = useSafeAreaInsets();

  return (
    <Tabs.Navigator
      screenOptions={({route}) => ({
        headerShown: false,
        tabBarActiveTintColor: colors.blue,
        tabBarInactiveTintColor: colors.muted,
        tabBarStyle: {
          backgroundColor: '#FFFFFF',
          borderTopColor: colors.line,
          height: 64 + insets.bottom,
          paddingTop: 7,
          paddingBottom: 7 + insets.bottom,
        },
        tabBarLabelStyle: {fontSize: 11, fontWeight: '600'},
        tabBarIcon: ({color, size}) => <TabIcon name={route.name} color={color} size={size} />,
      })}>
      <Tabs.Screen name="Início" component={HomeScreen} />
      <Tabs.Screen name="Clientes" component={ClientsScreen} />
      <Tabs.Screen name="Fotos" component={PhotosScreen} />
      <Tabs.Screen name="Catálogo" component={CatalogScreen} />
    </Tabs.Navigator>
  );
}

export default function App() {
  return (
    <SafeAreaProvider initialMetrics={initialWindowMetrics ?? {frame: {x: 0, y: 0, width: Dimensions.get('window').width, height: Dimensions.get('window').height}, insets: {top: 0, right: 0, bottom: 0, left: 0}}}>
      <StatusBar barStyle="dark-content" />
      <AppStateProvider>
        <DraftProvider>
          <NavigationContainer>
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

