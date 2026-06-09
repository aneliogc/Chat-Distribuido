namespace ChatService.Models;

/// <summary>
/// Tipo de conversa. Direct = 1:1 (privada), Group = 1:N (vários participantes).
/// </summary>
public enum ConversationType
{
    Direct,
    Group
}
