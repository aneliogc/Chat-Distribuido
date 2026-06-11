import React, { createContext, useContext, useEffect, useState } from 'react';
import * as authApi from '../api/auth';
import { saveSession, loadSession, clearSession } from '../storage/session';

// Context = jeito do React de compartilhar um estado com toda a arvore de telas
// sem ficar passando props de mao em mao. Aqui ele guarda quem esta logado.
const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  // session = null (deslogado) ou { userId, username, email, token, expiresAt }.
  const [session, setSession] = useState(null);
  // loading = true enquanto verificamos se ha uma sessao salva (no boot do app).
  const [loading, setLoading] = useState(true);

  // Ao abrir o app: tenta recuperar a sessao salva e checa se o token expirou.
  useEffect(() => {
    (async () => {
      const saved = await loadSession();
      if (saved && new Date(saved.expiresAt) > new Date()) {
        setSession(saved);
      } else if (saved) {
        await clearSession(); // token vencido -> descarta
      }
      setLoading(false);
    })();
  }, []);

  // Loga, guarda a sessao e atualiza o estado (as telas reagem sozinhas).
  async function signIn(usernameOrEmail, password) {
    const data = await authApi.login(usernameOrEmail, password);
    await saveSession(data);
    setSession(data);
  }

  // Registra. NAO loga automaticamente: o usuario volta pra tela de login
  // e entra com as credenciais que acabou de criar (decisao do projeto).
  async function signUp(username, email, password) {
    await authApi.register(username, email, password);
  }

  // Desloga: limpa o armazenamento e zera o estado.
  async function signOut() {
    await clearSession();
    setSession(null);
  }

  const value = { session, loading, signIn, signUp, signOut };
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// Atalho para as telas usarem: const { session, signIn, ... } = useAuth();
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth deve ser usado dentro de <AuthProvider>');
  return ctx;
}
