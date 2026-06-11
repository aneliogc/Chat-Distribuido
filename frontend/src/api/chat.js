import { CHAT_URL } from '../config';

// Mesma ideia do api/auth.js, mas falando com o ChatService (via Nginx, :5002)
// e enviando o token JWT no cabecalho Authorization (rotas [Authorize]).

function extractError(data, status) {
  if (data?.error) return data.error;
  if (data?.title) return data.title;
  return `Erro ${status}`;
}

// GET autenticado: anexa o Bearer token e trata sucesso/erro de forma uniforme.
async function getJson(path, token) {
  const res = await fetch(`${CHAT_URL}${path}`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  const text = await res.text();
  const data = text ? JSON.parse(text) : null;

  if (!res.ok) throw new Error(extractError(data, res.status));
  return data;
}

// Lista as conversas do usuario logado (mais recentes primeiro).
// Retorna: [{ id, type, name, participants: [{userId, username}], lastMessageAt }]
export function getConversations(token) {
  return getJson('/api/conversations', token);
}

// Carrega o historico de mensagens de uma conversa.
export function getMessages(token, conversationId, limit = 100) {
  return getJson(`/api/conversations/${conversationId}/messages?limit=${limit}`, token);
}