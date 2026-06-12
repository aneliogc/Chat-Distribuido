using ChatService.Models;
using MongoDB.Driver;

namespace ChatService.Data;

public class ChatRepository : IChatRepository
{
    private readonly MongoContext _ctx;

    public ChatRepository(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<Conversation> GetOrCreateDirectAsync(Participant me, Participant other, CancellationToken ct = default)
    {
        // Procura uma conversa Direct que contenha exatamente os dois usuarios.
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.Type, ConversationType.Direct),
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == me.UserId),
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == other.UserId));

        var existing = await _ctx.Conversations.Find(filter).FirstOrDefaultAsync(ct);
        if (existing is not null)
            return existing;

        var conversation = new Conversation
        {
            Type = ConversationType.Direct,
            Participants = new List<Participant> { me, other },
            CreatedAt = DateTime.UtcNow
        };

        await _ctx.Conversations.InsertOneAsync(conversation, cancellationToken: ct);
        return conversation;
    }

    public async Task<Conversation> CreateGroupAsync(string name, IReadOnlyList<Participant> participants, CancellationToken ct = default)
    {
        var conversation = new Conversation
        {
            Type = ConversationType.Group,
            Name = name,
            Participants = participants.ToList(),
            CreatedAt = DateTime.UtcNow
        };

        await _ctx.Conversations.InsertOneAsync(conversation, cancellationToken: ct);
        return conversation;
    }

    public async Task<Conversation?> GetConversationAsync(string conversationId, CancellationToken ct = default)
    {
        return await _ctx.Conversations
            .Find(c => c.Id == conversationId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Conversation>> GetConversationsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        // So lista conversas que ja tem mensagem (LastMessageAt preenchido).
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == userId),
            Builders<Conversation>.Filter.Ne(c => c.LastMessageAt, null));
        return await _ctx.Conversations
            .Find(filter)
            .SortByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);
    }

    public async Task<bool> IsParticipantAsync(string conversationId, Guid userId, CancellationToken ct = default)
    {
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.Id, conversationId),
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == userId));

        return await _ctx.Conversations.Find(filter).AnyAsync(ct);
    }

    public async Task<Message> AddMessageAsync(Message message, CancellationToken ct = default)
    {
        await _ctx.Messages.InsertOneAsync(message, cancellationToken: ct);

        // Atualiza o "carimbo" da conversa para ela subir na lista.
        var update = Builders<Conversation>.Update.Set(c => c.LastMessageAt, message.SentAt);
        await _ctx.Conversations.UpdateOneAsync(c => c.Id == message.ConversationId, update, cancellationToken: ct);

        return message;
    }

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(string conversationId, int limit = 100, CancellationToken ct = default)
    {
        return await _ctx.Messages
            .Find(m => m.ConversationId == conversationId)
            .SortBy(m => m.SentAt)
            .Limit(limit)
            .ToListAsync(ct);
    }
}
