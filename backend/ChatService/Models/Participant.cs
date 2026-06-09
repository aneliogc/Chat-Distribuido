namespace ChatService.Models;

/// <summary>
/// Um participante de uma conversa. Guardamos o username junto do id
/// para exibir o nome sem precisar consultar o AuthService a cada mensagem.
/// </summary>
public class Participant
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
}
