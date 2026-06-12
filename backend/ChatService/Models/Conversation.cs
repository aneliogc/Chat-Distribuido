using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatService.Models;

/// <summary>
/// Uma conversa entre participantes. Vira uma "sala" (grupo do SignalR)
/// identificada pelo Id. Serve tanto para 1:1 (Direct) quanto 1:N (Group).
/// Colecao MongoDB: "conversations".
/// </summary>
public class Conversation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public ConversationType Type { get; set; }

    /// <summary>Nome do grupo (apenas para conversas do tipo Group).</summary>
    public string? Name { get; set; }

    public List<Participant> Participants { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageAt { get; set; }
}
