import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, TouchableOpacity, StyleSheet,
  ActivityIndicator, RefreshControl,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import { useRealtime } from '../realtime/RealtimeContext';
import { getConversations } from '../api/chat';
import GroupIcon from '../components/GroupIcon';
import PlusIcon  from '../components/PlusIcon';

// Nome a exibir na lista:
// - Grupo  -> o nome do grupo.
// - Direct -> o nome do OUTRO participante.
function displayName(conv, myUserId) {
  if (conv.type === 'Group') return conv.name || 'Grupo';
  const other = conv.participants.find((p) => p.userId !== myUserId);
  return other?.username || 'Conversa';
}

// Primeira letra para o "avatar" colorido.
function initial(name) {
  return (name || '?').trim().charAt(0).toUpperCase();
}

// Mostra so a hora (HH:MM) se for hoje; senao a data (DD/MM).
function formatTime(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  const now = new Date();
  const sameDay = d.toDateString() === now.toDateString();
  return sameDay
    ? d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    : d.toLocaleDateString([], { day: '2-digit', month: '2-digit' });
}

export default function ConversationsScreen({ navigation }) {
  const { session, signOut } = useAuth();
  const { connection } = useRealtime();
  const [conversations, setConversations] = useState([]);
  const [loading, setLoading] = useState(true);   // carregamento inicial
  const [refreshing, setRefreshing] = useState(false); // pull-to-refresh
  const [error, setError] = useState('');

  // Busca a lista no backend. `isRefresh` controla qual spinner mostrar.
  const load = useCallback(async (isRefresh = false) => {
    setError('');
    if (isRefresh) setRefreshing(true);
    else setLoading(true);
    try {
      const data = await getConversations(session.token);
      setConversations(data);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [session.token]);

  // Recarrega sempre que a tela ganha foco (inclusive ao voltar do chat
  // ou depois de criar uma conversa nova).
  useEffect(() => {
    const unsub = navigation.addListener('focus', () => load());
    return unsub;
  }, [navigation, load]);

  // Tempo real: recarrega quando uma conversa e criada OU quando chega uma
  // mensagem. A 1a mensagem faz a conversa "aparecer" na lista (antes disso
  // ela fica oculta); as mensagens seguintes so reordenam pelo mais recente.
  useEffect(() => {
    if (!connection) return;
    const reload = () => load();
    connection.on('ConversationCreated', reload);
    connection.on('ReceiveMessage', reload);
    return () => {
      connection.off('ConversationCreated', reload);
      connection.off('ReceiveMessage', reload);
    };
  }, [connection, load]);

  function renderItem({ item }) {
    const name = displayName(item, session.userId);
    return (
      <TouchableOpacity
        style={styles.row}
        activeOpacity={0.7}
        onPress={() => navigation.navigate('Chat', {
          conversationId: item.id,
          title: name,
          isGroup: item.type === 'Group',
          participants: item.participants,
        })}
      >
        <View style={[styles.avatar, item.type === 'Group' && styles.avatarGroup]}>
          {item.type === 'Group' ? (
            <GroupIcon size={40} color="#fff" />
          ) : (
            <Text style={styles.avatarText}>{initial(name)}</Text>
          )}
      </View>
        <View style={styles.rowBody}>
          <Text style={styles.rowName} numberOfLines={1}>{name}</Text>
          <Text style={styles.rowSub} numberOfLines={1}>
            {item.type === 'Group' ? `${item.participants.length} participantes` : 'Conversa direta'}
          </Text>
        </View>
        <Text style={styles.rowTime}>{formatTime(item.lastMessageAt)}</Text>
      </TouchableOpacity>
    );
  }

  return (
    <View style={styles.screen}>
      {/* Cabecalho fixo */}
      <View style={styles.header}>
        <View>
          <Text style={styles.headerTitle}>Suas conversas</Text>
        </View>
        <View style={styles.headerActions}>
          <TouchableOpacity style={styles.logoutBtn} onPress={signOut} >
            <Text style={styles.logoutText}>Sair</Text>
          </TouchableOpacity>
        </View>
      </View>

      {/* Corpo: spinner inicial, erro, lista vazia ou a lista */}
      {loading ? (
        <View style={styles.center}>
          <ActivityIndicator color="#818cf8" />
        </View>
      ) : error ? (
        <View style={styles.center}>
          <Text style={styles.errorText}>{error}</Text>
          <TouchableOpacity style={styles.retryBtn} onPress={() => load()}>
            <Text style={styles.retryText}>Tentar de novo</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <FlatList
          data={conversations}
          keyExtractor={(item) => item.id}
          renderItem={renderItem}
          contentContainerStyle={conversations.length === 0 && styles.center}
          ItemSeparatorComponent={() => <View style={styles.separator} />}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => load(true)}
              tintColor="#818cf8"
            />
          }
          ListEmptyComponent={
            <View style={styles.emptyWrap}>
              <Text style={styles.emptyTitle}>Nenhuma conversa ainda</Text>
              <Text style={styles.emptySub}>Inicie uma nova conversa para comecar.</Text>
            </View>
          }
        />
      )}
      <TouchableOpacity
        style={styles.newBtn}
        onPress={() => navigation.navigate('NewConversation')}
      >
        <PlusIcon size={25} color="#fff" />
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#0a0a0f' },
  center: { flexGrow: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },

  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: 56,
    paddingBottom: 16,
    paddingHorizontal: 20,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(255,255,255,0.08)',
  },
  headerTitle: { fontSize: 22, fontWeight: 'bold', color: '#f1f5f9' },
  headerActions: { flexDirection: 'row', alignItems: 'center' },
  newBtn: {
    position: 'absolute',
    right: 20,
    bottom: 20,
    width: 50,
    height: 50,
    borderRadius: 18,
    backgroundColor: '#6366f1',
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 10,
  },
  logoutBtn: {
    paddingVertical: 8,
    paddingHorizontal: 14,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#6366f1',
  },
  logoutText: { color: '#94a3b8', fontSize: 13, fontWeight: '500' },

  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 14,
    paddingHorizontal: 20,
  },
  avatar: {
    width: 46,
    height: 46,
    borderRadius: 23,
    backgroundColor: '#6366f1',
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 14,
  },
  avatarGroup: { backgroundColor: '#303040' },
  avatarText: { color: '#fff', fontSize: 18, fontWeight: '600' },
  rowBody: { flex: 1 },
  rowName: { color: '#e2e8f0', fontSize: 16, fontWeight: '500' },
  rowSub: { color: '#64748b', fontSize: 13, marginTop: 2 },
  rowTime: { color: '#475569', fontSize: 12, marginLeft: 8 },
  separator: { height: 1, backgroundColor: 'rgba(255,255,255,0.06)', marginLeft: 80 },

  emptyWrap: { alignItems: 'center' },
  emptyTitle: { color: '#94a3b8', fontSize: 16, fontWeight: '500' },
  emptySub: { color: '#475569', fontSize: 13, marginTop: 6, textAlign: 'center' },

  errorText: { color: '#f87171', fontSize: 14, textAlign: 'center', marginBottom: 16 },
  retryBtn: { backgroundColor: '#6366f1', paddingVertical: 10, paddingHorizontal: 20, borderRadius: 8 },
  retryText: { color: '#fff', fontSize: 14, fontWeight: '600' },
});