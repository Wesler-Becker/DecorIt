import React from 'react';
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import {ChevronLeft, ChevronRight, House, Images, Moon, Sun, Plus, Sparkles, Users} from 'lucide-react-native';
import type {NativeStackNavigationProp} from '@react-navigation/native-stack';
import type {RootStackParamList} from '../types';
import {useTheme, radius, type ThemeColors} from '../theme';

type Nav = NativeStackNavigationProp<RootStackParamList>;

export function Page({
  title,
  subtitle,
  navigation,
  children,
  scroll = true,
  action,
}: React.PropsWithChildren<{
  title: string;
  subtitle?: string;
  navigation?: Nav;
  scroll?: boolean;
  action?: React.ReactNode;
}>) {
  const {colors, dark, toggle, saving} = useTheme();
  const styles = React.useMemo(() => createStyles(colors), [colors]);
  return (
    <SafeAreaView edges={['top', 'bottom']} style={styles.safe}>
      <View style={styles.pageHeader}>
        {navigation ? (
          <Pressable onPress={() => navigation.goBack()} style={styles.backButton} accessibilityRole="button" accessibilityLabel="Voltar">
            <ChevronLeft color={colors.ink} size={28} />
          </Pressable>
        ) : null}
        <View style={styles.headerText}>
          <Text style={styles.pageTitle}>{title}</Text>
          {subtitle ? <Text style={styles.pageSubtitle}>{subtitle}</Text> : null}
        </View>
        {navigation ? <Pressable onPress={() => navigation.navigate('MainTabs', {screen: 'Início'})} style={styles.homeButton} accessibilityRole="button" accessibilityLabel="Ir para o início"><House color={colors.blue} size={22} /></Pressable> : null}
        {action}
        <Pressable onPress={toggle} disabled={saving} style={styles.themeButton} accessibilityRole="button" accessibilityLabel={dark ? 'Ativar tema claro' : 'Ativar tema escuro'}>
          {dark ? <Sun color={colors.blue} size={22} /> : <Moon color={colors.blue} size={22} />}
        </Pressable>
      </View>
      <KeyboardAvoidingView style={styles.body} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        {scroll ? (
          <ScrollView contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
            {children}
          </ScrollView>
        ) : children}
      </KeyboardAvoidingView>
      {navigation ? <View style={styles.quickNav}>
        <QuickNavItem label="Início" icon={<House size={22} color={colors.blue} />} onPress={() => navigation.navigate('MainTabs', {screen: 'Início'})} />
        <QuickNavItem label="Clientes" icon={<Users size={22} color={colors.muted} />} onPress={() => navigation.navigate('MainTabs', {screen: 'Clientes'})} />
        <QuickNavItem label="Fotos" icon={<Images size={22} color={colors.muted} />} onPress={() => navigation.navigate('MainTabs', {screen: 'Fotos'})} />
        <QuickNavItem label="Catálogo" icon={<Sparkles size={22} color={colors.muted} />} onPress={() => navigation.navigate('MainTabs', {screen: 'Catálogo'})} />
      </View> : null}
    </SafeAreaView>
  );
}

function QuickNavItem({label, icon, onPress}: {label: string; icon: React.ReactNode; onPress: () => void}) {
  const {colors} = useTheme();
  const styles = React.useMemo(() => createStyles(colors), [colors]);
  return <Pressable onPress={onPress} style={styles.quickNavItem} accessibilityRole="button" accessibilityLabel={`Abrir ${label}`}>
    {icon}<Text style={styles.quickNavLabel}>{label}</Text>
  </Pressable>;
}

export function SectionTitle({children, trailing}: React.PropsWithChildren<{trailing?: React.ReactNode}>) {
  const {colors} = useTheme();
  const styles = React.useMemo(() => createStyles(colors), [colors]);
  return <View style={styles.sectionRow}><Text style={styles.sectionTitle}>{children}</Text>{trailing}</View>;
}

export function Card({children, style}: React.PropsWithChildren<{style?: object}>) {
  const {colors} = useTheme();
  const styles = React.useMemo(() => createStyles(colors), [colors]);
  return <View style={[styles.card, style]}>{children}</View>;
}

export function Button({
  label,
  onPress,
  variant = 'primary',
  icon,
  disabled = false,
  loading = false,
}: {
  label: string;
  onPress: () => void;
  variant?: 'primary' | 'secondary' | 'quiet' | 'danger';
  icon?: React.ReactNode;
  disabled?: boolean;
  loading?: boolean;
}) {
  const {colors} = useTheme();
  const styles = React.useMemo(() => createStyles(colors), [colors]);
  const buttonStyle = variant === 'primary' ? styles.primaryButton : variant === 'danger' ? styles.dangerButton : variant === 'quiet' ? styles.quietButton : styles.secondaryButton;
  const textStyle = variant === 'primary' || variant === 'danger' ? styles.primaryButtonText : variant === 'quiet' ? styles.quietButtonText : styles.secondaryButtonText;
  return (
    <Pressable onPress={onPress} disabled={disabled || loading} accessibilityRole="button" style={({pressed}) => [styles.button, buttonStyle, pressed && !disabled ? styles.pressed : null, disabled ? styles.disabled : null]}>
      {loading ? <ActivityIndicator color={variant === 'primary' ? colors.onPrimary : colors.blue} /> : icon}
      <Text style={textStyle}>{label}</Text>
    </Pressable>
  );
}

export function TextField({
  label,
  value,
  onChangeText,
  placeholder,
  multiline = false,
  keyboardType,
  required = false,
  editable = true,
}: {
  label: string;
  value: string;
  onChangeText: (text: string) => void;
  placeholder?: string;
  multiline?: boolean;
  keyboardType?: 'default' | 'phone-pad' | 'decimal-pad';
  required?: boolean;
  editable?: boolean;
}) {
  const {colors} = useTheme();
  const styles = React.useMemo(() => createStyles(colors), [colors]);
  return (
    <View style={styles.fieldWrap}>
      <Text style={styles.fieldLabel}>{label}{required ? <Text style={styles.required}> *</Text> : null}</Text>
      <TextInput
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder || 'Digite aqui'}
        placeholderTextColor={colors.muted}
        multiline={multiline}
        keyboardType={keyboardType}
        editable={editable}
        textAlignVertical={multiline ? 'top' : 'center'}
        style={[styles.textInput, multiline ? styles.multiline : null, !editable ? styles.readonly : null]}
        accessibilityLabel={label}
      />
    </View>
  );
}

export function EmptyState({title, description, action}: {title: string; description: string; action?: React.ReactNode}) {
  const {colors} = useTheme();
  const styles = React.useMemo(() => createStyles(colors), [colors]);
  return <View style={styles.emptyState}><View style={styles.emptyMark}><Plus size={22} color={colors.blue} /></View><Text style={styles.emptyTitle}>{title}</Text><Text style={styles.emptyDescription}>{description}</Text>{action}</View>;
}

export function LinkRow({label, onPress, detail}: {label: string; onPress: () => void; detail?: string}) {
  const {colors} = useTheme();
  const styles = React.useMemo(() => createStyles(colors), [colors]);
  return <Pressable onPress={onPress} style={({pressed}) => [styles.linkRow, pressed && styles.pressed]}><View style={styles.linkText}><Text style={styles.linkLabel}>{label}</Text>{detail ? <Text style={styles.linkDetail}>{detail}</Text> : null}</View><ChevronRight size={20} color={colors.muted} /></Pressable>;
}

export function Pill({label, selected = false, onPress}: {label: string; selected?: boolean; onPress?: () => void}) {
  const {colors} = useTheme();
  const styles = React.useMemo(() => createStyles(colors), [colors]);
  return <Pressable disabled={!onPress} onPress={onPress} style={[styles.pill, selected ? styles.pillSelected : null]}><Text style={[styles.pillText, selected ? styles.pillTextSelected : null]}>{label}</Text></Pressable>;
}

const createStyles = (colors: ThemeColors) => StyleSheet.create({
  themeButton: {width: 44, height: 44, borderRadius: 22, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.bluePale},
  safe: {flex: 1, backgroundColor: colors.background},
  pageHeader: {minHeight: 70, paddingHorizontal: 16, paddingVertical: 12, flexDirection: 'row', alignItems: 'center', gap: 12},
  backButton: {width: 46, height: 46, borderRadius: 18, backgroundColor: colors.surface, alignItems: 'center', justifyContent: 'center', borderWidth: 1, borderColor: colors.line},
  homeButton: {width: 46, height: 46, borderRadius: 18, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  headerText: {flex: 1},
  pageTitle: {fontSize: 22, lineHeight: 28, color: colors.ink, fontWeight: '800', letterSpacing: -0.35},
  pageSubtitle: {fontSize: 13, lineHeight: 18, color: colors.muted, marginTop: 2},
  body: {flex: 1},
  quickNav: {minHeight: 66, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-around', backgroundColor: colors.surface, borderTopWidth: 1, borderTopColor: colors.line, paddingHorizontal: 6},
  quickNavItem: {minWidth: 68, minHeight: 58, alignItems: 'center', justifyContent: 'center', gap: 3},
  quickNavLabel: {width: '100%', textAlign: 'center', fontSize: 10, color: colors.muted, fontWeight: '700'},
  scrollContent: {paddingHorizontal: 16, paddingTop: 4, paddingBottom: 28, gap: 16},
  sectionRow: {flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: 2, marginBottom: 2},
  sectionTitle: {flex: 1, fontSize: 18, lineHeight: 24, fontWeight: '800', color: colors.ink},
  card: {backgroundColor: colors.surface, borderRadius: radius.card, padding: 16, borderWidth: 1, borderColor: colors.line},
  button: {minHeight: 50, borderRadius: 999, paddingHorizontal: 24, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 9},
  primaryButton: {backgroundColor: colors.blue},
  secondaryButton: {backgroundColor: colors.bluePale, borderWidth: 1, borderColor: colors.line},
  quietButton: {backgroundColor: 'transparent'},
  dangerButton: {backgroundColor: colors.danger},
  primaryButtonText: {flex: 1, textAlign: 'center', lineHeight: 22, color: colors.onPrimary, fontSize: 15, fontWeight: '800'},
  secondaryButtonText: {flex: 1, textAlign: 'center', lineHeight: 22, color: colors.blueDeep, fontSize: 15, fontWeight: '800'},
  quietButtonText: {flex: 1, textAlign: 'center', lineHeight: 22, color: colors.blue, fontSize: 14, fontWeight: '800'},
  pressed: {opacity: 0.75, transform: [{scale: 0.99}]},
  disabled: {opacity: 0.45},
  fieldWrap: {gap: 7},
  fieldLabel: {fontSize: 14, color: colors.ink, fontWeight: '700'},
  required: {color: colors.danger},
  textInput: {minHeight: 52, backgroundColor: colors.surface, borderRadius: 18, borderWidth: 1, borderColor: colors.line, paddingHorizontal: 14, color: colors.ink, fontSize: 15},
  multiline: {minHeight: 96, paddingTop: 13},
  readonly: {backgroundColor: colors.variant, color: colors.muted},
  emptyState: {alignItems: 'center', paddingVertical: 30, paddingHorizontal: 24, gap: 9},
  emptyMark: {width: 48, height: 48, borderRadius: 22, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center', marginBottom: 2},
  emptyTitle: {fontSize: 16, color: colors.ink, fontWeight: '800', textAlign: 'center'},
  emptyDescription: {fontSize: 13, color: colors.muted, lineHeight: 19, textAlign: 'center'},
  linkRow: {flexDirection: 'row', alignItems: 'center', paddingVertical: 12, gap: 10},
  linkText: {flex: 1, gap: 4},
  linkLabel: {color: colors.ink, fontSize: 15, fontWeight: '700'},
  linkDetail: {color: colors.muted, fontSize: 12},
  pill: {alignSelf: 'flex-start', borderRadius: radius.pill, paddingHorizontal: 12, paddingVertical: 7, backgroundColor: colors.variant},
  pillSelected: {backgroundColor: colors.blue},
  pillText: {fontSize: 12, color: colors.muted, fontWeight: '700'},
  pillTextSelected: {color: colors.onPrimary},
});
