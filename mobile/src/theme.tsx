import React, {createContext, useContext, useEffect, useState} from 'react';
import {Alert} from 'react-native';
import {loadThemePreference, saveThemePreference} from './data/clientStorage';

export const lightColors = {
  ink: '#17242D', muted: '#5C6A75', blue: '#286889', blueDeep: '#1F526E',
  bluePale: '#E8F1F5', background: '#F5F7F8', surface: '#FFFFFF',
  line: '#E3E9EC', variant: '#E3E9EC', gold: '#E5B962', goldPale: '#FFF5DC',
  green: '#3C8068', greenPale: '#E9F3EE', danger: '#B54848', shadow: '#183342',
  onPrimary: '#FFFFFF', hero: '#286889',
};
export type ThemeColors = typeof lightColors;
export const darkColors: ThemeColors = {
  ink: '#EFF3F6', muted: '#9FB1BD', blue: '#8EC7E8', blueDeep: '#D4EBFA',
  bluePale: '#193E54', background: '#0D161C', surface: '#16232C',
  line: '#283946', variant: '#23333F', gold: '#E5B962', goldPale: '#40351F',
  green: '#87CDB3', greenPale: '#193D33', danger: '#FFB4AB', shadow: '#000000',
  onPrimary: '#0B2B3E', hero: '#1F526E',
};
const ThemeContext = createContext({colors: lightColors, dark: false, saving: false, toggle: () => {}});
export function ThemeProvider({children}: React.PropsWithChildren) {
  const [dark, setDark] = useState(false);
  const [saving, setSaving] = useState(true);
  useEffect(() => {
    let active = true;
    void loadThemePreference().then(value => {if (active) setDark(value === 'dark');})
      .catch(() => undefined).finally(() => {if (active) setSaving(false);});
    return () => {active = false;};
  }, []);
  const toggle = () => {
    if (saving) return;
    setSaving(true);
    void saveThemePreference(dark ? 'light' : 'dark').then(() => setDark(!dark))
      .catch(() => Alert.alert('Não foi possível alterar o tema', 'Tente novamente.'))
      .finally(() => setSaving(false));
  };
  return <ThemeContext.Provider value={{colors: dark ? darkColors : lightColors, dark, saving, toggle}}>{children}</ThemeContext.Provider>;
}
export const useTheme = () => useContext(ThemeContext);
export const radius = {small: 10, card: 22, large: 28, pill: 999};
