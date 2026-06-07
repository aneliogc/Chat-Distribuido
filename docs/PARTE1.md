# Parte 1 — AuthService + PostgreSQL + docker-compose

## O que foi entregue

- Estrutura do monorepo (`backend/`, `mobile/`, `tests/`, `docker-compose.yml`).
- **AuthService** em ASP.NET Core 8.0:
  - `POST /api/auth/register` — cria usuário (username, email, password). Hash bcrypt.
  - `POST /api/auth/login` — valida credenciais e devolve JWT (expira em 120 min).
  - `GET  /api/auth/me` — devolve dados do usuário autenticado.
  - `GET  /api/auth/users` — lista usuários (autenticado), usado depois pelo chat.
  - `GET  /health` — healthcheck.
  - Swagger em `http://localhost:5001/swagger`.
- **PostgreSQL** containerizado, schema criado automaticamente no boot (`EnsureCreated`).
- **docker-compose.yml** sobe postgres + authservice juntos.

## Como rodar e testar

### 1. Subir tudo

Na raiz do projeto:

```powershell
docker compose up -d --build
```

Primeira vez demora alguns minutos (baixa imagens do Postgres e SDK do .NET).

### 2. Verificar logs

```powershell
docker compose logs -f authservice
```

Você deve ver `Now listening on: http://[::]:8080` e nenhum erro de conexão.

### 3. Testar a API

**Swagger (interface web):** http://localhost:5001/swagger

**Ou via curl no PowerShell:**

```powershell
# Health
curl http://localhost:5001/health

# Registrar
curl -X POST http://localhost:5001/api/auth/register `
  -H "Content-Type: application/json" `
  -d '{\"username\":\"alice\",\"email\":\"alice@example.com\",\"password\":\"senha123\"}'

# Login (salva o token)
$res = curl -X POST http://localhost:5001/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{\"usernameOrEmail\":\"alice\",\"password\":\"senha123\"}' | ConvertFrom-Json
$token = $res.token

# Me
curl http://localhost:5001/api/auth/me -H "Authorization: Bearer $token"

# Listar usuários
curl http://localhost:5001/api/auth/users -H "Authorization: Bearer $token"
```

### 4. Inspecionar o banco (opcional)

```powershell
docker exec -it sd-postgres psql -U chat -d authdb -c "SELECT id, username, email, created_at FROM users;"
```

### 5. Parar tudo

```powershell
docker compose down       # mantém os dados
docker compose down -v    # apaga o volume do Postgres
```

## Critérios de aceite (Parte 1)

- [ ] `docker compose up -d --build` sobe sem erro.
- [ ] `GET /health` responde 200.
- [ ] `POST /api/auth/register` cria usuário e devolve JWT.
- [ ] `POST /api/auth/login` com a senha certa devolve JWT; com senha errada, 401.
- [ ] `GET /api/auth/me` sem token devolve 401; com token devolve o usuário.
- [ ] Reiniciar containers preserva os usuários (persistência funcionando).

## Próximo passo

Após validar essa parte, partimos pra **Parte 2: ChatService (SignalR + MongoDB + Redis)**.
