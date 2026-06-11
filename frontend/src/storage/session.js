import AsyncStorage from '@react-native-async-storage/async-storage';

// AsyncStorage = armazenamento chave-valor persistente do dispositivo
// (no navegador, usa o localStorage por baixo). Guardamos a sessao inteira
// como JSON para o usuario continuar logado mesmo fechando o app.
const KEY = 'sd-chat-session';

// session = { userId, username, email, token, expiresAt }
export async function saveSession(session) {
  await AsyncStorage.setItem(KEY, JSON.stringify(session));
}

export async function loadSession() {
  const raw = await AsyncStorage.getItem(KEY);
  return raw ? JSON.parse(raw) : null;
}

export async function clearSession() {
  await AsyncStorage.removeItem(KEY);
}