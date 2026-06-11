import React, { createContext, useContext, useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { createChatConnection } from './connection';

// Mantem UMA conexao SignalR enquanto o usuario estiver logado.
// Assim a tela de chat (e, depois, a lista) compartilham a mesma conexao,
// em vez de cada tela abrir a sua.
const RealtimeContext = createContext(null);

export function RealtimeProvider({ children }) {
  const { session } = useAuth();
  // connection = a conexao em si (ou null); status = 'connecting'|'connected'|'disconnected'.
  const [connection, setConnection] = useState(null);
  const [status, setStatus] = useState('connecting');

  useEffect(() => {
    // Sem sessao (deslogado): nao ha conexao.
    if (!session) {
      setConnection(null);
      return;
    }

    const conn = createChatConnection(() => session.token);

    // Reflete no estado o que esta acontecendo com a conexao.
    conn.onreconnecting(() => setStatus('connecting'));
    conn.onreconnected(() => setStatus('connected'));
    conn.onclose(() => setStatus('disconnected'));

    // Expoe a conexao JA (antes do start) para as telas registrarem handlers
    // cedo e nao perderem mensagens.
    setConnection(conn);
    setStatus('connecting');

    let active = true;
    (async () => {
      try {
        await conn.start();
        if (active) setStatus('connected');
      } catch {
        if (active) setStatus('disconnected');
      }
    })();

    // Ao deslogar ou trocar de sessao: encerra a conexao.
    return () => {
      active = false;
      conn.stop();
      setConnection(null);
    };
  }, [session]);

  return (
    <RealtimeContext.Provider value={{ connection, status }}>
      {children}
    </RealtimeContext.Provider>
  );
}

// Atalho para as telas: const { connection, status } = useRealtime();
export function useRealtime() {
  const ctx = useContext(RealtimeContext);
  if (!ctx) throw new Error('useRealtime deve ser usado dentro de <RealtimeProvider>');
  return ctx;
}