import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { CHAT_URL } from '../config';

// Monta (sem iniciar) a conexao com o ChatHub.
// - URL do hub: mesma do Nginx (:5002) + caminho "/hub/chat".
// - accessTokenFactory: o SignalR chama esta funcao e anexa o token na query
//   string (?access_token=...), que e como o WebSocket manda o JWT (o backend
//   le de la, ver Program.cs do ChatService).
// - withAutomaticReconnect: tenta reconectar sozinho se a conexao cair.
export function createChatConnection(getToken) {
  return new HubConnectionBuilder()
    .withUrl(`${CHAT_URL}/hub/chat`, {
      accessTokenFactory: () => getToken(),
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}
