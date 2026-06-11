import { AUTH_URL } from '../config';

// Extrai uma mensagem de erro legivel das respostas do backend.
// - Erros de regra de negocio voltam como { error: "..." } (ex.: 409, 401).
// - Erros de validacao (ASP.NET) voltam como { title, errors: { campo: [msgs] } }.
function extractError(data, status) {
  if (data?.error) return data.error;
  if (data?.errors) {
    const first = Object.values(data.errors)[0];
    if (Array.isArray(first) && first.length) return first[0];
  }
  if (data?.title) return data.title;
  return `Erro ${status}`;
}

// Faz um POST com JSON e ja trata sucesso/erro de forma uniforme.
async function postJson(path, body) {
  const res = await fetch(`${AUTH_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  const text = await res.text();
  const data = text ? JSON.parse(text) : null;

  if (!res.ok) throw new Error(extractError(data, res.status));
  return data;
}

// GET autenticado (Bearer token) -- usado para listar usuarios.
async function getJson(path, token) {
  const res = await fetch(`${AUTH_URL}${path}`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  const text = await res.text();
  const data = text ? JSON.parse(text) : null;

  if (!res.ok) throw new Error(extractError(data, res.status));
  return data;
}

// Lista os usuarios cadastrados.
// Retorna: [{ id, username, email }]
export function getUsers(token) {
  return getJson('/api/auth/users', token);
}

// Registra um novo usuario:
export function register(username, email, password) {
  return postJson('/api/auth/register', { username, email, password });
}

// Faz login. Retorna { userId, username, email, token, expiresAt }.
export function login(usernameOrEmail, password) {
  return postJson('/api/auth/login', { usernameOrEmail, password });
}