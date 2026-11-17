import React from 'react';
import {Alert} from 'react-native';
import {act, create, type ReactTestRenderer} from 'react-test-renderer';
import {ThemeProvider, useTheme} from './theme';
import {loadThemePreference, saveThemePreference} from './data/clientStorage';

jest.mock('./data/clientStorage', () => ({loadThemePreference: jest.fn(), saveThemePreference: jest.fn()}));
const load = jest.mocked(loadThemePreference);
const save = jest.mocked(saveThemePreference);
let current: ReturnType<typeof useTheme>;
let tree: ReactTestRenderer;
function Probe() {current = useTheme(); return null;}
beforeEach(() => {jest.clearAllMocks(); load.mockResolvedValue('light'); save.mockResolvedValue(undefined);});
afterEach(async () => {if (tree) await act(async () => tree.unmount());});

it('recupera o tema escuro salvo ao abrir o app', async () => {
  load.mockResolvedValue('dark');
  await act(async () => {tree = create(<ThemeProvider><Probe /></ThemeProvider>);});
  expect(current.dark).toBe(true);
  expect(current.colors.background).toBe('#0D161C');
});

it('salva a escolha antes de atualizar as cores da interface', async () => {
  await act(async () => {tree = create(<ThemeProvider><Probe /></ThemeProvider>);});
  await act(async () => current.toggle());
  expect(save).toHaveBeenCalledWith('dark');
  expect(current.dark).toBe(true);
  await act(async () => current.toggle());
  expect(save).toHaveBeenLastCalledWith('light');
  expect(current.dark).toBe(false);
});

it('mantém o tema anterior e avisa quando a gravação falha', async () => {
  const alert = jest.spyOn(Alert, 'alert').mockImplementation(() => {});
  save.mockRejectedValue(new Error('sqlite indisponível'));
  await act(async () => {tree = create(<ThemeProvider><Probe /></ThemeProvider>);});
  await act(async () => current.toggle());
  expect(current.dark).toBe(false);
  expect(current.saving).toBe(false);
  expect(alert).toHaveBeenCalled();
  alert.mockRestore();
});
