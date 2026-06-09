using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatService.Models;

/// <summary>
/// Uma mensagem trocada dentro de uma conversa.
/// Colecao MongoDB: "messages".
/// </summary>
public class Message
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>A qual conversa esta mensagem pertence (liga em Conversation.Id).</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = string.Empty;

    public Guid SenderId { get; set; }

    public string SenderUsername { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
