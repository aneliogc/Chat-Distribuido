import React, { useEffect, useRef, useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, FlatList, StyleSheet,
  ActivityIndicator, KeyboardAvoidingView, Platform,
} from 'react-native';
import { useAuth } from '../context/AuthContext';
import { useRealtime } from '../realtime/RealtimeContext';
import { getMessages } from '../api/chat';

function formatTime(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  const hhmm = d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  // Hoje -> so a hora. Outros dias -> data + hora (pra saber de quando foi).
  const sameDay = d.toDateString() === new Date().toDateString();
  if (sameDay) return hhmm;
  return `${d.toLocaleDateString([], { day: '2-digit', month: '2-digit' })} ${hhmm}`;
}

export default function ChatScreen({ route, navigation }) {
  const { conversationId, title, isGroup } = route.params;
  const { session } = useAuth();
  const { connection, status } = useRealtime();

  const [messages, setMessages] = useState([]);
  const [text, setText] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [sending, setSending] = useState(false);
  const listRef = useRef(null);

  // 1) Carrega o historico via REST quando a tela abre.
  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const data = await getMessages(session.token, conversationId);
        if (active) setMessages(data);
      } catch (e) {
        if (active) setError(e.message);
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, [conversationId, session.token]);

  // 2) Escuta mensagens em tempo real. O backend envia "ReceiveMessage" para
  //    TODOS os participantes (inclusive quem enviou), entao nao adicionamos
  //    a mensagem na hora de enviar -> evitamos duplicata e confiamos no eco.
  useEffect(() => {
    if (!connection) return;
    const handler = (msg) => {
      if (msg.conversationId !== conversationId) return; // so desta conversa
      setMessages((prev) => [...prev, msg]);
    };
    connection.on('ReceiveMessage', handler);
    return () => connection.off('ReceiveMessage', handler);
  }, [connection, conversationId]);

  async function send() {
    const content = text.trim();
    if (!content || sending) return;
    if (status !== 'connected') {
      setError('Sem conexao com o servidor. Tente novamente.');
      return;
    }
    setError('');
    setText('');
    setSending(true);
    try {
      // Servidor persiste e devolve via "ReceiveMessage" (tratado acima).
      await connection.invoke('SendMessage', conversationId, content);
    } catch (e) {
      setError(e.message);
      setText(content); // devolve o texto para o usuario nao perder
    } finally {
      setSending(false);
    }
  }

  function renderItem({ item }) {
    const mine = item.senderId === session.userId;
    return (
      <View style={[styles.bubbleRow, mine ? styles.rowRight : styles.rowLeft]}>
        <View style={[styles.bubble, mine ? styles.bubbleMine : styles.bubbleOther]}>
          {/* Em grupo, mostra quem mandou (so nas mensagens dos outros). */}
          {isGroup && !mine
            ? <Text style={styles.sender}>{item.senderUsername}</Text>
            : null}
          <Text style={styles.bubbleText}>{item.content}</Text>
          <Text style={styles.bubbleTime}>{formatTime(item.sentAt)}</Text>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.screen}>
      {/* Cabecalho custom: voltar + titulo + indicador de conexao */}
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backBtn}>
          <Text style={styles.backIcon}>‹</Text>
        </TouchableOpacity>
        <View style={styles.headerCenter}>
          <Text style={styles.headerTitle} numberOfLines={1}>{title}</Text>
          <Text style={styles.headerStatus}>
            {status === 'connected' ? 'online'
              : status === 'connecting' ? 'conectando...'
              : 'offline'}
          </Text>
        </View>
        <View style={styles.backBtn} />
      </View>

      {loading ? (
        <View style={styles.center}>
          <ActivityIndicator color="#818cf8" />
        </View>
      ) : (
        <KeyboardAvoidingView
          style={styles.flex}
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        >
          <FlatList
            ref={listRef}
            data={messages}
            keyExtractor={(item) => item.id}
            renderItem={renderItem}
            contentContainerStyle={styles.listContent}
            onContentSizeChange={() => listRef.current?.scrollToEnd({ animated: true })}
            ListEmptyComponent={
              <Text style={styles.empty}>Nenhuma mensagem ainda. Diga ola!</Text>
            }
          />

          {error ? <Text style={styles.error}>{error}</Text> : null}

          {/* Barra de envio */}
          <View style={styles.inputBar}>
            <TextInput
              style={styles.input}
              placeholder="Mensagem"
              placeholderTextColor="#475569"
              value={text}
              onChangeText={setText}
              onSubmitEditing={send}
              returnKeyType="send"
              multiline
            />
            <TouchableOpacity
              style={[styles.sendBtn, (!text.trim() || sending) && styles.sendBtnDisabled]}
              onPress={send}
              disabled={!text.trim() || sending}
            >
              <Text style={styles.sendIcon}>➤</Text>
            </TouchableOpacity>
          </View>
        </KeyboardAvoidingView>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#0a0a0f' },
  flex: { flex: 1 },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },

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
  headerCenter: { flex: 1, alignItems: 'center' },
  headerTitle: { color: '#f1f5f9', fontSize: 17, fontWeight: '600' },
  headerStatus: { color: '#64748b', fontSize: 12, marginTop: 2 },

  listContent: { padding: 12, flexGrow: 1 },
  empty: { color: '#475569', textAlign: 'center', marginTop: 40, fontSize: 14 },

  bubbleRow: { flexDirection: 'row', marginVertical: 3 },
  rowRight: { justifyContent: 'flex-end' },
  rowLeft: { justifyContent: 'flex-start' },
  bubble: { maxWidth: '78%', borderRadius: 16, paddingHorizontal: 12, paddingVertical: 8 },
  bubbleMine: { backgroundColor: '#6366f1', borderBottomRightRadius: 4 },
  bubbleOther: { backgroundColor: 'rgba(255,255,255,0.08)', borderBottomLeftRadius: 4 },
  sender: { color: '#a5b4fc', fontSize: 12, fontWeight: '600', marginBottom: 2 },
  bubbleText: { color: '#f1f5f9', fontSize: 15 },
  bubbleTime: { color: 'rgba(255,255,255,0.55)', fontSize: 10, alignSelf: 'flex-end', marginTop: 2 },

  error: { color: '#f87171', fontSize: 13, textAlign: 'center', paddingHorizontal: 12, paddingBottom: 4 },

  inputBar: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    padding: 10,
    borderTopWidth: 1,
    borderTopColor: 'rgba(255,255,255,0.08)',
  },
  input: {
    flex: 1,
    maxHeight: 120,
    backgroundColor: 'rgba(255,255,255,0.05)',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.08)',
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 10,
    color: '#e2e8f0',
    fontSize: 15,
  },
  sendBtn: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: '#6366f1',
    alignItems: 'center',
    justifyContent: 'center',
    marginLeft: 8,
  },
  sendBtnDisabled: { backgroundColor: 'rgba(99,102,241,0.4)' },
  sendIcon: { color: '#fff', fontSize: 18 },
});