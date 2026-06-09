using ChatService.Models;

namespace ChatService.Data;

public interface IChatRepository
{
    /// <summary>Acha a conversa 1:1 entre dois usuarios, ou cria se nao existir.</summary>
    Task<Conversation> GetOrCreateDirectAsync(Participant me, Participant other, CancellationToken ct = default);

    /// <summary>Cria uma conversa de grupo (1:N) com um nome e varios participantes.</summary>
    Task<Conversation> CreateGroupAsync(string name, IReadOnlyList<Participant> participants, CancellationToken ct = default);

    Task<Conversation?> GetConversationAsync(string conversationId, CancellationToken ct = default);

    /// <summary>Lista as conversas das quais o usuario participa, mais recentes primeiro.</summary>
    Task<IReadOnlyList<Conversation>> GetConversationsForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Verifica se o usuario participa da conversa (usado para autorizacao).</summary>
    Task<bool> IsParticipantAsync(string conversationId, Guid userId, CancellationToken ct = default);

    /// <summary>Persiste uma mensagem e atualiza o LastMessageAt da conversa.</summary>
    Task<Message> AddMessageAsync(Message message, CancellationToken ct = default);

    /// <summary>Busca o historico de mensagens de uma conversa (mais antigas primeiro).</summary>
    Task<IReadOnlyList<Message>> GetMessagesAsync(string conversationId, int limit = 100, CancellationToken ct = default);
}
