using ChatService.Models;

namespace ChatService.Dtos;

public record ParticipantDto(string UserId, string Username);

public record MessageDto(
    string Id,
    string ConversationId,
    string SenderId,
    string SenderUsername,
    string Content,
    DateTime SentAt);

public record ConversationDto(
    string Id,
    string Type,
    string? Name,
    IReadOnlyList<ParticipantDto> Participants,
    DateTime? LastMessageAt);

/// <summary>Metodos para converter os modelos do Mongo nos DTOs do cliente.</summary>
public static class ChatMappings
{
    public static MessageDto ToDto(this Message m) => new(
        m.Id, m.ConversationId, m.SenderId.ToString(), m.SenderUsername, m.Content, m.SentAt);

    public static ParticipantDto ToDto(this Participant p) => new(p.UserId.ToString(), p.Username);

    public static ConversationDto ToDto(this Conversation c) => new(
        c.Id,
        c.Type.ToString(),
        c.Name,
        c.Participants.Select(p => p.ToDto()).ToList(),
        c.LastMessageAt);
}
