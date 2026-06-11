import { Platform } from 'react-native';

// IP da sua maquina na rede local (rode `ipconfig` se mudar de rede).
// Usado quando o app roda no CELULAR (Expo Go), que nao enxerga "localhost".
const LAN_IP = '192.168.2.41';

// No navegador (web), "localhost" aponta para o proprio PC -> funciona direto.
// No celular, precisamos do IP da maquina na rede Wi-Fi.
// (Se um dia usar emulador Android, troque por '10.0.2.2'.)
const HOST = Platform.OS === 'web' ? 'localhost' : LAN_IP;

// Microsservicos -- mesmas portas do docker-compose.
export const AUTH_URL = `http://${HOST}:5001`; // AuthService (login/registro)
export const CHAT_URL = `http://${HOST}:5002`; // ChatService via Nginx (REST + WebSocket)
