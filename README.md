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
- ChatService (via Nginx): http://localhost:5002
- PostgreSQL: localhost:5432 (user `chat`, pass `chat`)

Para parar:

```powershell
docker compose down
```

Para apagar dados:

```powershell
docker compose down -v
```

## Como rodar (frontend)

O app é em React Native + Expo. Com o backend já no ar (passo acima):

```powershell
cd frontend
npm install
npm run web        # abre no navegador (usa http://localhost:5001 e :5002)
```

Outras formas de abrir o app:

```powershell
npm start          # abre o Expo Dev Tools (escaneie o QR com o Expo Go)
npm run android    # emulador/dispositivo Android
npm run ios        # simulador iOS (macOS)
```

## Estrutura

```
SD_TP_Final/
├── backend/
│   ├── AuthService/         # ASP.NET Core - autenticação + cadastro
│   └── ChatService/         # ASP.NET Core - SignalR + mensagens (Parte 2)
├── frontend/                # React Native + Expo (Parte 4)
├── infra/
│   └── nginx/               # Configuração do Nginx / load balancer (Parte 3)
├── docs/                    # Documentação das partes (PARTE1.md, ...)
├── docker-compose.yml
└── README.md
```
