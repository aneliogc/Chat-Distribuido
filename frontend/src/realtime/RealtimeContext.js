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
  // Conjunto de userIds online
  const [onlineUsers, setOnlineUsers] = useState(() => new Set());

  useEffect(() => {
    // Sem sessao (deslogado): nao ha conexao.
    if (!session) {
      setConnection(null);
      setOnlineUsers(new Set());
      return;
    }

    const conn = createChatConnection(() => session.token);

    // Busca a lista de quem esta online agora e a guarda no estado.
    const seedOnline = async () => {
      try {
        const ids = await conn.invoke('GetOnlineUsers');
        setOnlineUsers(new Set(ids.map((id) => id.toLowerCase())));
      } catch {
        // sem presenca disponivel: deixa como esta
      }
    };

    // Presenca: o servidor avisa quando alguem entra/sai.
    const onOnline = (userId) =>
      setOnlineUsers((prev) => new Set(prev).add(userId.toLowerCase()));
    const onOffline = (userId) =>
      setOnlineUsers((prev) => {
        const next = new Set(prev);
        next.delete(userId.toLowerCase());
        return next;
      });
    conn.on('UserOnline', onOnline);
    conn.on('UserOffline', onOffline);

    // Reflete no estado o que esta acontecendo com a conexao.
    conn.onreconnecting(() => setStatus('connecting'));
    conn.onreconnected(() => { setStatus('connected'); seedOnline(); });
    conn.onclose(() => setStatus('disconnected'));

    // Expoe a conexao JA (antes do start) para as telas registrarem handlers
    // cedo e nao perderem mensagens.
    setConnection(conn);
    setStatus('connecting');

    let active = true;
    (async () => {
      try {
        await conn.start();
        if (active) {
          setStatus('connected');
          await seedOnline();
        }
      } catch {
        if (active) setStatus('disconnected');
      }
    })();

    // Ao deslogar ou trocar de sessao: encerra a conexao.
    return () => {
      active = false;
      conn.off('UserOnline', onOnline);
      conn.off('UserOffline', onOffline);
      conn.stop();
      setConnection(null);
    };
  }, [session]);

  // Helper para as telas: o usuario com este id esta online?
  const isOnline = (userId) => !!userId && onlineUsers.has(String(userId).toLowerCase());

  return (
    <RealtimeContext.Provider value={{ connection, status, onlineUsers, isOnline }}>
      {children}
    </RealtimeContext.Provider>
  );
}

// Atalho para as telas: const { connection, status, isOnline } = useRealtime();
export function useRealtime() {
  const ctx = useContext(RealtimeContext);
  if (!ctx) throw new Error('useRealtime deve ser usado dentro de <RealtimeProvider>');
  return ctx;
}