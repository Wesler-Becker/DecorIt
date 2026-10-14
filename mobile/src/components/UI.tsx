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
import {ChevronLeft, ChevronRight, House, Images, Plus, Sparkles, Users} from 'lucide-react-native';
import type {NativeStackNavigationProp} from '@react-navigation/native-stack';
import type {RootStackParamList} from '../types';
import {colors, radius} from '../theme';

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
  return <Pressable onPress={onPress} style={styles.quickNavItem} accessibilityRole="button" accessibilityLabel={`Abrir ${label}`}>
    {icon}<Text style={styles.quickNavLabel}>{label}</Text>
  </Pressable>;
}

export function SectionTitle({children, trailing}: React.PropsWithChildren<{trailing?: React.ReactNode}>) {
  return <View style={styles.sectionRow}><Text style={styles.sectionTitle}>{children}</Text>{trailing}</View>;
}

export function Card({children, style}: React.PropsWithChildren<{style?: object}>) {
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
  const buttonStyle = variant === 'primary' ? styles.primaryButton : variant === 'danger' ? styles.dangerButton : variant === 'quiet' ? styles.quietButton : styles.secondaryButton;
  const textStyle = variant === 'primary' || variant === 'danger' ? styles.primaryButtonText : variant === 'quiet' ? styles.quietButtonText : styles.secondaryButtonText;
  return (
    <Pressable onPress={onPress} disabled={disabled || loading} accessibilityRole="button" style={({pressed}) => [styles.button, buttonStyle, pressed && !disabled ? styles.pressed : null, disabled ? styles.disabled : null]}>
      {loading ? <ActivityIndicator color={variant === 'primary' ? '#FFFFFF' : colors.blue} /> : icon}
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
  return (
    <View style={styles.fieldWrap}>
      <Text style={styles.fieldLabel}>{label}{required ? <Text style={styles.required}> *</Text> : null}</Text>
      <TextInput
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder || 'Digite aqui'}
        placeholderTextColor="#99A5AC"
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
  return <View style={styles.emptyState}><View style={styles.emptyMark}><Plus size={22} color={colors.blue} /></View><Text style={styles.emptyTitle}>{title}</Text><Text style={styles.emptyDescription}>{description}</Text>{action}</View>;
}

export function LinkRow({label, onPress, detail}: {label: string; onPress: () => void; detail?: string}) {
  return <Pressable onPress={onPress} style={({pressed}) => [styles.linkRow, pressed && styles.pressed]}><View style={styles.linkText}><Text style={styles.linkLabel}>{label}</Text>{detail ? <Text style={styles.linkDetail}>{detail}</Text> : null}</View><ChevronRight size={20} color={colors.muted} /></Pressable>;
}

export function Pill({label, selected = false, onPress}: {label: string; selected?: boolean; onPress?: () => void}) {
  return <Pressable disabled={!onPress} onPress={onPress} style={[styles.pill, selected ? styles.pillSelected : null]}><Text style={[styles.pillText, selected ? styles.pillTextSelected : null]}>{label}</Text></Pressable>;
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: colors.background},
  pageHeader: {minHeight: 70, paddingHorizontal: 20, paddingVertical: 12, flexDirection: 'row', alignItems: 'center', gap: 12},
  backButton: {width: 46, height: 46, borderRadius: 14, backgroundColor: colors.surface, alignItems: 'center', justifyContent: 'center', borderWidth: 1, borderColor: colors.line},
  homeButton: {width: 46, height: 46, borderRadius: 14, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center'},
  headerText: {flex: 1},
  pageTitle: {fontSize: 22, lineHeight: 28, color: colors.ink, fontWeight: '800', letterSpacing: -0.35},
  pageSubtitle: {fontSize: 13, lineHeight: 18, color: colors.muted, marginTop: 2},
  body: {flex: 1},
  quickNav: {minHeight: 66, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-around', backgroundColor: colors.surface, borderTopWidth: 1, borderTopColor: colors.line, paddingHorizontal: 6},
  quickNavItem: {minWidth: 68, minHeight: 58, alignItems: 'center', justifyContent: 'center', gap: 3},
  quickNavLabel: {fontSize: 10, color: colors.muted, fontWeight: '700'},
  scrollContent: {paddingHorizontal: 20, paddingTop: 4, paddingBottom: 28, gap: 16},
  sectionRow: {flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: 2, marginBottom: 2},
  sectionTitle: {fontSize: 16, fontWeight: '800', color: colors.ink},
  card: {backgroundColor: colors.surface, borderRadius: radius.card, padding: 16, borderWidth: 1, borderColor: colors.line},
  button: {minHeight: 50, borderRadius: 15, paddingHorizontal: 17, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 9},
  primaryButton: {backgroundColor: colors.blue},
  secondaryButton: {backgroundColor: colors.bluePale, borderWidth: 1, borderColor: '#D5E4EB'},
  quietButton: {backgroundColor: 'transparent'},
  dangerButton: {backgroundColor: colors.danger},
  primaryButtonText: {color: '#FFFFFF', fontSize: 15, fontWeight: '800'},
  secondaryButtonText: {color: colors.blueDeep, fontSize: 15, fontWeight: '800'},
  quietButtonText: {color: colors.blue, fontSize: 14, fontWeight: '800'},
  pressed: {opacity: 0.75, transform: [{scale: 0.99}]},
  disabled: {opacity: 0.45},
  fieldWrap: {gap: 7},
  fieldLabel: {fontSize: 13, color: colors.ink, fontWeight: '700'},
  required: {color: colors.danger},
  textInput: {minHeight: 50, backgroundColor: colors.surface, borderRadius: 14, borderWidth: 1, borderColor: colors.line, paddingHorizontal: 14, color: colors.ink, fontSize: 15},
  multiline: {minHeight: 96, paddingTop: 13},
  readonly: {backgroundColor: '#EFF2F3', color: colors.muted},
  emptyState: {alignItems: 'center', paddingVertical: 30, paddingHorizontal: 24, gap: 9},
  emptyMark: {width: 48, height: 48, borderRadius: 17, backgroundColor: colors.bluePale, alignItems: 'center', justifyContent: 'center', marginBottom: 2},
  emptyTitle: {fontSize: 16, color: colors.ink, fontWeight: '800', textAlign: 'center'},
  emptyDescription: {fontSize: 13, color: colors.muted, lineHeight: 19, textAlign: 'center'},
  linkRow: {flexDirection: 'row', alignItems: 'center', paddingVertical: 12, gap: 10},
  linkText: {flex: 1, gap: 4},
  linkLabel: {color: colors.ink, fontSize: 15, fontWeight: '700'},
  linkDetail: {color: colors.muted, fontSize: 12},
  pill: {alignSelf: 'flex-start', borderRadius: radius.pill, paddingHorizontal: 12, paddingVertical: 7, backgroundColor: '#EFF2F3'},
  pillSelected: {backgroundColor: colors.blue},
  pillText: {fontSize: 12, color: colors.muted, fontWeight: '700'},
  pillTextSelected: {color: '#FFFFFF'},
});
