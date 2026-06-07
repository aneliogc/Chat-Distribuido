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

## Estrutura

```
SD_TP_Final/
├── backend/
│   ├── AuthService/         # ASP.NET Core - autenticação + cadastro
│   └── ChatService/         # ASP.NET Core - SignalR + mensagens (Parte 2)
├── mobile/                  # React Native + Expo (Parte 4)
├── tests/                   # xUnit + k6 (Parte 5)
├── nginx/                   # Configuração Nginx (Parte 3)
├── docker-compose.yml
└── README.md
```
