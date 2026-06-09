using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Realtime;

/// <summary>
/// Diz ao SignalR qual e o "id de usuario" de cada conexao, lendo o claim
/// "sub" do JWT (que e o Guid do usuario emitido pelo AuthService).
/// E o que permite usar Clients.Users(userId) para entregar mensagens
/// a um usuario especifico, mesmo entre varias replicas (via Redis).
/// </summary>
public class NameUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? connection.User?.FindFirstValue("sub");
    }
}
