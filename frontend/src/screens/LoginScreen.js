import React, { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet, ActivityIndicator,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import ChatIcon from '../components/ChatIcon';

export default function LoginScreen({ navigation, route }) {
  const { signIn } = useAuth();
  const [usernameOrEmail, setUsernameOrEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  // Controla a cor da borda do campo em foco (RN nao tem :focus do CSS).
  const [focusedField, setFocusedField] = useState(null);

  // Aviso de sucesso quando o usuario acabou de se registrar (vindo do Register).
  const justRegistered = route?.params?.justRegistered;

  async function handleLogin() {
    setError('');
    if (!usernameOrEmail.trim() || !password) {
      setError('Preencha usuario/email e senha.');
      return;
    }
    setLoading(true);
    try {
      await signIn(usernameOrEmail.trim(), password);
      // Nao navegamos manualmente: quando a sessao muda, o App.js troca
      // automaticamente para as telas de quem esta logado.
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <View style={styles.screen}>
      <View style={styles.card}>
        <View style={styles.header}>
          <View style={styles.logoBox}>
            <ChatIcon size={28} color="#fff" />
          </View>
          <Text style={styles.subtitle}>Entre na sua conta para continuar</Text>
        </View>

        {justRegistered
          ? <Text style={styles.success}>Conta criada! Faca login para entrar.</Text>
          : null}

        {/* Campo: usuario ou e-mail */}
        <Text style={styles.label}>Usuario ou e-mail</Text>
        <View style={[
          styles.inputWrap,
          focusedField === 'user' && styles.inputWrapFocused,
        ]}>
          <Text style={styles.inputIcon}>👤</Text>
          <TextInput
            style={styles.input}
            placeholder="Digite seu usuario ou e-mail"
            placeholderTextColor="#475569"
            autoCapitalize="none"
            value={usernameOrEmail}
            onChangeText={setUsernameOrEmail}
            onFocus={() => setFocusedField('user')}
            onBlur={() => setFocusedField(null)}
          />
        </View>

        {/* Campo: senha */}
        <Text style={[styles.label, styles.labelSpacing]}>Senha</Text>
        <View style={[
          styles.inputWrap,
          focusedField === 'pass' && styles.inputWrapFocused,
        ]}>
          <Text style={styles.inputIcon}>🔒</Text>
          <TextInput
            style={styles.input}
            placeholder="••••••••"
            placeholderTextColor="#475569"
            secureTextEntry={!showPassword}
            value={password}
            autoCapitalize="none"
            autoCorrect={false}
            onChangeText={setPassword}
            onFocus={() => setFocusedField('pass')}
            onBlur={() => setFocusedField(null)}
          />
          <TouchableOpacity onPress={() => setShowPassword(!showPassword)}>
            <Text style={styles.toggle}>{showPassword ? 'Ocultar' : 'Mostrar'}</Text>
          </TouchableOpacity>
        </View>

        {error ? <Text style={styles.error}>{error}</Text> : null}

        {/* Botao Entrar */}
        <TouchableOpacity
          style={[styles.button, loading && styles.buttonDisabled]}
          onPress={handleLogin}
          disabled={loading}
        >
          {loading
            ? <ActivityIndicator color="#fff" />
            : <Text style={styles.buttonText}>Entrar</Text>}
        </TouchableOpacity>

        <View style={styles.footer}>
          <Text style={styles.footerText}>Nao tem uma conta? </Text>
          <TouchableOpacity onPress={() => navigation.navigate('Register')}>
            <Text style={styles.footerLink}>Criar conta</Text>
          </TouchableOpacity>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 16,
    backgroundColor: '#0a0a0f',
  },
  card: {
    width: '100%',
    maxWidth: 384,
    borderRadius: 16,
    padding: 28,
    backgroundColor: 'rgba(255,255,255,0.04)',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.08)',
  },
  header: {
    alignItems: 'center',
    marginBottom: 28,
  },
  logoBox: {
    width: 50,
    height: 50,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 10,
    backgroundColor: '#6366f1',
  },
  logoIcon: { fontSize: 20 },
  title: { fontSize: 24, fontWeight: 'bold', color: '#f1f5f9' },
  subtitle: { fontSize: 14, color: '#64748b', marginTop: 6 },
  label: { fontSize: 12, fontWeight: '500', color: '#94a3b8', marginBottom: 6 },
  labelSpacing: { marginTop: 16 },
  inputWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 8,
    paddingHorizontal: 12,
    backgroundColor: 'rgba(255,255,255,0.05)',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.08)',
  },
  inputWrapFocused: { borderColor: 'rgba(99,102,241,0.6)' },
  inputIcon: { fontSize: 14, marginRight: 8, color: '#475569' },
  input: {
    flex: 1,
    paddingVertical: 10,
    fontSize: 14,
    color: '#e2e8f0',
  },
  toggle: { color: '#818cf8', fontSize: 12, fontWeight: '500', paddingLeft: 8 },
  error: { color: '#f87171', marginTop: 12, textAlign: 'center', fontSize: 13 },
  success: { color: '#4ade80', marginBottom: 12, textAlign: 'center', fontSize: 13 },
  button: {
    backgroundColor: '#6366f1',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    marginTop: 20,
  },
  buttonDisabled: { backgroundColor: 'rgba(99,102,241,0.5)' },
  buttonText: { color: '#fff', fontSize: 14, fontWeight: '600' },
  footer: {
    flexDirection: 'row',
    justifyContent: 'center',
    marginTop: 24,
  },
  footerText: { color: '#475569', fontSize: 12 },
  footerLink: { color: '#818cf8', fontSize: 12, fontWeight: '500' },
});
