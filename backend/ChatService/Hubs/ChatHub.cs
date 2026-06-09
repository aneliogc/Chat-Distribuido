using System.Security.Claims;
using ChatService.Data;
using ChatService.Dtos;
using ChatService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Hubs;

/// <summary>
/// Hub SignalR do chat. So aceita conexoes autenticadas (JWT).
/// Metodos publicos = o que o cliente chama no servidor.
/// Clients.Users(...).SendAsync(...) = o servidor empurrando eventos pro cliente.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatRepository _repo;

    public ChatHub(IChatRepository repo)
    {
        _repo = repo;
    }

    /// <summary>Identidade do usuario conectado, extraida do JWT.</summary>
    private Participant CurrentUser()
    {
        var idStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? Context.User?.FindFirstValue("sub");
        var username = Context.User?.FindFirstValue("unique_name")
                       ?? Context.User?.FindFirstValue(ClaimTypes.Name)
                       ?? "desconhecido";

        if (!Guid.TryParse(idStr, out var id))
            throw new HubException("Token invalido: id de usuario ausente.");

        return new Participant { UserId = id, Username = username };
    }

    /// <summary>
    /// Inicia (ou reabre) uma conversa privada 1:1 com outro usuario.
    /// Retorna a conversa e avisa o outro usuario que ela existe.
    /// </summary>
    public async Task<ConversationDto> StartDirect(string otherUserId, string otherUsername)
    {
        var me = CurrentUser();

        if (!Guid.TryParse(otherUserId, out var otherId))
            throw new HubException("otherUserId invalido.");
        if (otherId == me.UserId)
            throw new HubException("Nao da para iniciar conversa consigo mesmo.");

        var other = new Participant { UserId = otherId, Username = otherUsername };
        var conv = await _repo.GetOrCreateDirectAsync(me, other);

        // Avisa o outro participante (se estiver online) que a conversa existe.
        await Clients.Users(otherUserId).SendAsync("ConversationCreated", conv.ToDto());

        return conv.ToDto();
    }

    /// <summary>
    /// Cria uma conversa de grupo (1:N). O criador entra automaticamente.
    /// </summary>
    public async Task<ConversationDto> CreateGroup(string name, List<ParticipantDto> participants)
    {
        var me = CurrentUser();

        if (string.IsNullOrWhiteSpace(name))
            throw new HubException("O grupo precisa de um nome.");

        var list = participants
            .Select(p => new Participant { UserId = Guid.Parse(p.UserId), Username = p.Username })
            .ToList();

        if (list.All(p => p.UserId != me.UserId))
            list.Add(me); // garante o criador como participante

        var conv = await _repo.CreateGroupAsync(name.Trim(), list);

        // Avisa os demais participantes online.
        var others = list.Where(p => p.UserId != me.UserId)
                         .Select(p => p.UserId.ToString())
                         .ToList();
        if (others.Count > 0)
            await Clients.Users(others).SendAsync("ConversationCreated", conv.ToDto());

        return conv.ToDto();
    }

    /// <summary>
    /// Envia uma mensagem para uma conversa (serve para 1:1 e 1:N).
    /// Persiste no MongoDB e entrega em tempo real a todos os participantes.
    /// </summary>
    public async Task<MessageDto> SendMessage(string conversationId, string content)
    {
        var me = CurrentUser();

        if (string.IsNullOrWhiteSpace(content))
            throw new HubException("Mensagem vazia.");

        var conv = await _repo.GetConversationAsync(conversationId)
                   ?? throw new HubException("Conversa nao encontrada.");

        // Autorizacao: so participantes podem enviar.
        if (conv.Participants.All(p => p.UserId != me.UserId))
            throw new HubException("Voce nao participa desta conversa.");

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = me.UserId,
            SenderUsername = me.Username,
            Content = content.Trim(),
            SentAt = DateTime.UtcNow
        };

        await _repo.AddMessageAsync(message);

        // Entrega em tempo real a TODOS os participantes (inclusive o remetente,
        // util para sincronizar varios dispositivos). Entre replicas, o Redis
        // garante que o evento chegue a quem estiver conectado em outra instancia.
        var recipients = conv.Participants.Select(p => p.UserId.ToString()).ToList();
        await Clients.Users(recipients).SendAsync("ReceiveMessage", message.ToDto());

        return message.ToDto();
    }
}
