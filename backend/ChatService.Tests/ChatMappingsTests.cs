using ChatService.Dtos;
using ChatService.Models;
using Xunit;

namespace ChatService.Tests;

public class ChatMappingsTests
{
    [Fact]
    public void Message_ToDto_DeveMapearTodosOsCampos()
    {
        var senderId = Guid.NewGuid();
        var msg = new Message
        {
            Id = "507f1f77bcf86cd799439011",
            ConversationId = "507f191e810c19729de860ea",
            SenderId = senderId,
            SenderUsername = "alice",
            Content = "ola",
            SentAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        var dto = msg.ToDto();

        Assert.Equal(msg.Id, dto.Id);
        Assert.Equal(msg.ConversationId, dto.ConversationId);
        Assert.Equal(senderId.ToString(), dto.SenderId);
        Assert.Equal("alice", dto.SenderUsername);
        Assert.Equal("ola", dto.Content);
        Assert.Equal(msg.SentAt, dto.SentAt);
    }

    [Fact]
    public void Conversation_ToDto_Direct_DeveConverterParticipantes()
    {
        var conv = new Conversation
        {
            Id = "507f191e810c19729de860ea",
            Type = ConversationType.Direct,
            Participants = new List<Participant>
            {
                new() { UserId = Guid.NewGuid(), Username = "alice" },
                new() { UserId = Guid.NewGuid(), Username = "bob" },
            },
        };

        var dto = conv.ToDto();

        Assert.Equal("Direct", dto.Type);
        Assert.Null(dto.Name);
        Assert.Equal(2, dto.Participants.Count);
        Assert.Contains(dto.Participants, p => p.Username == "alice");
    }

    [Fact]
    public void Conversation_ToDto_Group_DevePreservarNome()
    {
        var conv = new Conversation
        {
            Id = "507f191e810c19729de860ea",
            Type = ConversationType.Group,
            Name = "Trabalho SD",
            Participants = new List<Participant>
            {
                new() { UserId = Guid.NewGuid(), Username = "alice" },
            },
            LastMessageAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        var dto = conv.ToDto();

        Assert.Equal("Group", dto.Type);
        Assert.Equal("Trabalho SD", dto.Name);
        Assert.Equal(conv.LastMessageAt, dto.LastMessageAt);
    }
}
