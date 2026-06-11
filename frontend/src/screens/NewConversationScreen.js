import React, { useEffect, useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, FlatList, StyleSheet, ActivityIndicator,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import { useRealtime } from '../realtime/RealtimeContext';
import { getUsers } from '../api/auth';

function initial(name) {
  return (name || '?').trim().charAt(0).toUpperCase();
}

export default function NewConversationScreen({ navigation }) {
  const { session } = useAuth();
  const { connection, status } = useRealtime();

  const [mode, setMode] = useState('direct'); // 'direct' | 'group'
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false); // criando/abrindo conversa

  const [search, setSearch] = useState('');
  const [groupName, setGroupName] = useState('');
  // Selecionados no modo grupo: { [userId]: user }
  const [selected, setSelected] = useState({});

  // Carrega a lista de usuarios ao abrir.
  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const data = await getUsers(session.token);
        if (active) setUsers(data);
      } catch (e) {
        if (active) setError(e.message);
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, [session.token]);

  // Filtro por nome/email (busca local).
  const filtered = users.filter((u) => {
    const q = search.trim().toLowerCase();
    if (!q) return true;
    return u.username.toLowerCase().includes(q) || u.email.toLowerCase().includes(q);
  });

  // Garante que ha conexao antes de chamar o hub.
  function ensureConnected() {
    if (status !== 'connected' || !connection) {
      setError('Sem conexao com o servidor. Aguarde e tente de novo.');
      return false;
    }
    return true;
  }

  // Modo DIRETA: toca num usuario -> abre (ou cria) a conversa 1:1 e vai pro chat.
  async function startDirect(user) {
    if (busy || !ensureConnected()) return;
    setError('');
    setBusy(true);
    try {
      const conv = await connection.invoke('StartDirect', user.id, user.username);
      navigation.replace('Chat', {
        conversationId: conv.id,
        title: user.username,
        isGroup: false,
      });
    } catch (e) {
      setError(e.message);
      setBusy(false);
    }
  }

  // Alterna selecao de um usuario no modo grupo.
  function toggle(user) {
    setSelected((prev) => {
      const next = { ...prev };
      if (next[user.id]) delete next[user.id];
      else next[user.id] = user;
      return next;
    });
  }

  // Modo GRUPO: cria o grupo com os selecionados + nome.
  async function createGroup() {
    if (busy || !ensureConnected()) return;
    const name = groupName.trim();
    const participants = Object.values(selected).map((u) => ({
      userId: u.id,
      username: u.username,
    }));
    if (!name) { setError('Da um nome ao grupo.'); return; }
    if (participants.length < 1) { setError('Escolha ao menos 1 participante.'); return; }

    setError('');
    setBusy(true);
    try {
      const conv = await connection.invoke('CreateGroup', name, participants);
      navigation.replace('Chat', {
        conversationId: conv.id,
        title: conv.name || name,
        isGroup: true,
      });
    } catch (e) {
      setError(e.message);
      setBusy(false);
    }
  }

  function renderItem({ item }) {
    const isSel = !!selected[item.id];
    return (
      <TouchableOpacity
        style={styles.row}
        activeOpacity={0.7}
        disabled={busy}
        onPress={() => (mode === 'direct' ? startDirect(item) : toggle(item))}
      >
        <View style={styles.avatar}>
          <Text style={styles.avatarText}>{initial(item.username)}</Text>
        </View>
        <View style={styles.rowBody}>
          <Text style={styles.rowName} numberOfLines={1}>{item.username}</Text>
          <Text style={styles.rowSub} numberOfLines={1}>{item.email}</Text>
        </View>
        {/* No modo grupo, mostra o "check" de selecao */}
        {mode === 'group'
          ? <View style={[styles.check, isSel && styles.checkOn]}>
              {isSel ? <Text style={styles.checkMark}>✓</Text> : null}
            </View>
          : null}
      </TouchableOpacity>
    );
  }

  const selectedCount = Object.keys(selected).length;

  return (
    <View style={styles.screen}>
      {/* Cabecalho */}
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backBtn}>
          <Text style={styles.backIcon}>‹</Text>
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Nova conversa</Text>
        <View style={styles.backBtn} />
      </View>

      {/* Alternancia Direta / Grupo */}
      <View style={styles.segment}>
        <TouchableOpacity
          style={[styles.segBtn, mode === 'direct' && styles.segBtnOn]}
          onPress={() => setMode('direct')}
        >
          <Text style={[styles.segText, mode === 'direct' && styles.segTextOn]}>Direta</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.segBtn, mode === 'group' && styles.segBtnOn]}
          onPress={() => setMode('group')}
        >
          <Text style={[styles.segText, mode === 'group' && styles.segTextOn]}>Grupo</Text>
        </TouchableOpacity>
      </View>

      {/* Nome do grupo (so no modo grupo) */}
      {mode === 'group' ? (
        <TextInput
          style={styles.groupInput}
          placeholder="Nome do grupo"
          placeholderTextColor="#475569"
          value={groupName}
          onChangeText={setGroupName}
        />
      ) : null}

      {/* Busca */}
      <TextInput
        style={styles.searchInput}
        placeholder="Buscar usuario..."
        placeholderTextColor="#475569"
        autoCapitalize="none"
        value={search}
        onChangeText={setSearch}
      />

      {error ? <Text style={styles.error}>{error}</Text> : null}

      {/* Lista de usuarios */}
      {loading ? (
        <View style={styles.center}><ActivityIndicator color="#818cf8" /></View>
      ) : (
        <FlatList
          data={filtered}
          keyExtractor={(item) => item.id}
          renderItem={renderItem}
          ItemSeparatorComponent={() => <View style={styles.separator} />}
          contentContainerStyle={filtered.length === 0 && styles.center}
          ListEmptyComponent={<Text style={styles.empty}>Nenhum usuario encontrado.</Text>}
        />
      )}

      {/* Botao criar grupo (so no modo grupo) */}
      {mode === 'group' ? (
        <TouchableOpacity
          style={[styles.createBtn, (busy || selectedCount === 0 || !groupName.trim()) && styles.createBtnDisabled]}
          onPress={createGroup}
          disabled={busy || selectedCount === 0 || !groupName.trim()}
        >
          {busy
            ? <ActivityIndicator color="#fff" />
            : <Text style={styles.createText}>Criar grupo ({selectedCount})</Text>}
        </TouchableOpacity>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#0a0a0f' },
  center: { flexGrow: 1, justifyContent: 'center', alignItems: 'center', padding: 24 },

  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingTop: 52,
    paddingBottom: 14,
    paddingHorizontal: 12,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(255,255,255,0.08)',
  },
  backBtn: { width: 40, alignItems: 'center' },
  backIcon: { color: '#818cf8', fontSize: 30, lineHeight: 30 },
  headerTitle: { flex: 1, textAlign: 'center', color: '#f1f5f9', fontSize: 17, fontWeight: '600' },

  segment: {
    flexDirection: 'row',
    margin: 16,
    backgroundColor: 'rgba(255,255,255,0.05)',
    borderRadius: 10,
    padding: 4,
  },
  segBtn: { flex: 1, paddingVertical: 8, borderRadius: 8, alignItems: 'center' },
  segBtnOn: { backgroundColor: '#6366f1' },
  segText: { color: '#94a3b8', fontSize: 14, fontWeight: '500' },
  segTextOn: { color: '#fff' },

  groupInput: {
    marginHorizontal: 16,
    marginBottom: 10,
    backgroundColor: 'rgba(255,255,255,0.05)',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.08)',
    borderRadius: 8,
    paddingHorizontal: 14,
    paddingVertical: 10,
    color: '#e2e8f0',
    fontSize: 15,
  },
  searchInput: {
    marginHorizontal: 16,
    marginBottom: 8,
    backgroundColor: 'rgba(255,255,255,0.05)',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.08)',
    borderRadius: 8,
    paddingHorizontal: 14,
    paddingVertical: 10,
    color: '#e2e8f0',
    fontSize: 14,
  },

  row: { flexDirection: 'row', alignItems: 'center', paddingVertical: 12, paddingHorizontal: 16 },
  avatar: {
    width: 44, height: 44, borderRadius: 22, backgroundColor: '#6366f1',
    alignItems: 'center', justifyContent: 'center', marginRight: 14,
  },
  avatarText: { color: '#fff', fontSize: 17, fontWeight: '600' },
  rowBody: { flex: 1 },
  rowName: { color: '#e2e8f0', fontSize: 16, fontWeight: '500' },
  rowSub: { color: '#64748b', fontSize: 13, marginTop: 2 },
  separator: { height: 1, backgroundColor: 'rgba(255,255,255,0.06)', marginLeft: 74 },

  check: {
    width: 24, height: 24, borderRadius: 12,
    borderWidth: 2, borderColor: 'rgba(255,255,255,0.2)',
    alignItems: 'center', justifyContent: 'center',
  },
  checkOn: { backgroundColor: '#6366f1', borderColor: '#6366f1' },
  checkMark: { color: '#fff', fontSize: 14, fontWeight: '700' },

  empty: { color: '#475569', fontSize: 14 },
  error: { color: '#f87171', fontSize: 13, textAlign: 'center', marginBottom: 6, paddingHorizontal: 16 },

  createBtn: {
    margin: 16,
    backgroundColor: '#6366f1',
    paddingVertical: 14,
    borderRadius: 10,
    alignItems: 'center',
  },
  createBtnDisabled: { backgroundColor: 'rgba(99,102,241,0.4)' },
  createText: { color: '#fff', fontSize: 15, fontWeight: '600' },
});