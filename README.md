# Sistema de Chat Distribuído

Trabalho Final da disciplina de Sistemas Distribuídos — CEFET-MG (2026/1).
Professora: Michelle Hanne.

## Arquitetura

```
+--------------------+
|  React Native App  |  (Expo)
+--------------------+
   |  HTTP (REST)        |  WebSocket (SignalR)
   v                     v
+--------------------+   +----------------------+
|    AuthService     |   |  Nginx Load Balancer |
|  (ASP.NET Core)    |   +----------------------+
+--------------------+        |        |        |
   |                          v        v        v
   v                       ChatService (N réplicas, ASP.NET Core + SignalR)
+--------------------+        |                          |
|    PostgreSQL      |        v                          v
+--------------------+    +-----------+            +------------+
                          |  MongoDB  |            |   Redis    |
                          | (mensagens|            | (backplane |
                          +-----------+            +------------+
```

## Stack

| Camada | Tecnologia |
|---|---|
| Mobile | React Native + Expo |
| Microsserviços | ASP.NET Core 8 (C#) |
| Tempo real | SignalR (WebSocket) |
| DB de usuários | PostgreSQL |
| DB de mensagens | MongoDB |
| Backplane SignalR | Redis |
| Load balancer | Nginx |
| Orquestração | Docker Compose |
| Testes | xUnit (unit/integração) + k6 (carga) |

## Como rodar (backend)

```powershell
docker compose up -d --build
```

- AuthService: http://localhost:5001/swagger
- ChatService (via Nginx): http://localhost:8000 
- PostgreSQL: localhost:5432 (user `chat`, pass `chat`)

Para parar:

```powershell
docker compose down
```

Para apagar dados:

```powershell
docker compose down -v
```

## Testes

Todos os testes em xUnit, executados via Docker (sem precisar de SDK local).

### Unitários

```powershell
docker run --rm -v "${PWD}/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test AuthService.Tests/AuthService.Tests.csproj
docker run --rm -v "${PWD}/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test ChatService.Tests/ChatService.Tests.csproj
```

- `AuthService.Tests`: registro, validação de credenciais de login e geração de JWT (EF Core InMemory).
- `ChatService.Tests`: persistência de mensagens, conversas 1:1 e grupo, autorização (MongoDB efêmero via EphemeralMongo) + mapeamento de DTOs.

### Integração e carga

Caixa-preta (HTTP/SignalR) contra o stack em execução. Suba o stack antes (`docker compose up -d --build`) e rode anexando o container à rede do compose:

```powershell
docker run --rm --network chat-distribuido_sd-net -v "${PWD}/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test IntegrationTests/IntegrationTests.csproj
```

- Integração: autenticar e, em seguida, enviar mensagem 1:1 entregue em tempo real e persistida; mensagem em grupo (1:N) entregue a todos; acesso ao chat sem token retorna 401; requisições REST distribuídas entre as réplicas.
- Carga/concorrência: 12 usuários simultâneos (configurável via `LOAD_USERS`) fazem login ao mesmo tempo e trocam mensagens em grupo simultaneamente, validando entrega em tempo real, persistência e balanceamento entre as réplicas.

## Estrutura

```
SD_TP_Final/
├── backend/
│   ├── AuthService/         # ASP.NET Core - autenticação + cadastro
│   ├── AuthService.Tests/   # xUnit - testes unitários do AuthService
│   ├── ChatService/         # ASP.NET Core - SignalR + mensagens
│   ├── ChatService.Tests/   # xUnit - testes unitários do ChatService
│   └── IntegrationTests/    # xUnit - testes de integração e carga (stack ao vivo)
├── frontend/                # React Native + Expo
├── infra/nginx/             # Configuração Nginx (load balancer)
├── docker-compose.yml
└── README.md
```
