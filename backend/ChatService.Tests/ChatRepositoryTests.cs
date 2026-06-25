using ChatService.Models;
using Xunit;

namespace ChatService.Tests;

public class ChatRepositoryTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _mongo;

    public ChatRepositoryTests(MongoFixture mongo) => _mongo = mongo;

    private static Participant User(string name) =>
        new() { UserId = Guid.NewGuid(), Username = name };

    [Fact]
    public async Task GetOrCreateDirect_DeveCriarUmaVez_E_ReusarDepois()
    {
        var repo = _mongo.NewRepository();
        var alice = User("alice");
        var bob = User("bob");

        var first = await repo.GetOrCreateDirectAsync(alice, bob);
        var second = await repo.GetOrCreateDirectAsync(bob, alice);

        Assert.Equal(ConversationType.Direct, first.Type);
        Assert.Equal(2, first.Participants.Count);
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateGroup_DevePersistir_ComNomeETodosOsParticipantes()
    {
        var repo = _mongo.NewRepository();
        var participantes = new List<Participant> { User("alice"), User("bob"), User("carol") };

        var group = await repo.CreateGroupAsync("Trabalho SD", participantes);

        Assert.Equal(ConversationType.Group, group.Type);
        Assert.Equal("Trabalho SD", group.Name);
        Assert.Equal(3, group.Participants.Count);

        var loaded = await repo.GetConversationAsync(group.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Trabalho SD", loaded!.Name);
    }

    [Fact]
    public async Task AddMessage_DevePersistirMensagem_E_AtualizarLastMessageAt()
    {
        var repo = _mongo.NewRepository();
        var alice = User("alice");
        var bob = User("bob");
        var conv = await repo.GetOrCreateDirectAsync(alice, bob);

        Assert.Null(conv.LastMessageAt);

        var msg = new Message
        {
            ConversationId = conv.Id,
            SenderId = alice.UserId,
            SenderUsername = alice.Username,
            Content = "Ola, Bob!",
            SentAt = DateTime.UtcNow,
        };

        var saved = await repo.AddMessageAsync(msg);

        Assert.False(string.IsNullOrEmpty(saved.Id));

        var history = await repo.GetMessagesAsync(conv.Id);
        var only = Assert.Single(history);
        Assert.Equal("Ola, Bob!", only.Content);
        Assert.Equal(alice.UserId, only.SenderId);

        var reloaded = await repo.GetConversationAsync(conv.Id);
        Assert.NotNull(reloaded!.LastMessageAt);
    }

    [Fact]
    public async Task GetMessages_DeveRetornarEmOrdemCronologica()
    {
        var repo = _mongo.NewRepository();
        var conv = await repo.GetOrCreateDirectAsync(User("alice"), User("bob"));
        var baseTime = DateTime.UtcNow;

        await repo.AddMessageAsync(new Message { ConversationId = conv.Id, Content = "terceira", SentAt = baseTime.AddSeconds(3) });
        await repo.AddMessageAsync(new Message { ConversationId = conv.Id, Content = "primeira", SentAt = baseTime.AddSeconds(1) });
        await repo.AddMessageAsync(new Message { ConversationId = conv.Id, Content = "segunda", SentAt = baseTime.AddSeconds(2) });

        var history = await repo.GetMessagesAsync(conv.Id);

        Assert.Equal(new[] { "primeira", "segunda", "terceira" }, history.Select(m => m.Content));
    }

    [Fact]
    public async Task GetMessages_DeveRespeitarOLimite()
    {
        var repo = _mongo.NewRepository();
        var conv = await repo.GetOrCreateDirectAsync(User("alice"), User("bob"));
        var baseTime = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
            await repo.AddMessageAsync(new Message { ConversationId = conv.Id, Content = $"m{i}", SentAt = baseTime.AddSeconds(i) });

        var history = await repo.GetMessagesAsync(conv.Id, limit: 3);

        Assert.Equal(3, history.Count);
    }

    [Fact]
    public async Task IsParticipant_DeveDistinguir_QuemParticipa()
    {
        var repo = _mongo.NewRepository();
        var alice = User("alice");
        var bob = User("bob");
        var estranho = User("estranho");
        var conv = await repo.GetOrCreateDirectAsync(alice, bob);

        Assert.True(await repo.IsParticipantAsync(conv.Id, alice.UserId));
        Assert.False(await repo.IsParticipantAsync(conv.Id, estranho.UserId));
    }

    [Fact]
    public async Task GetConversationsForUser_SoListaConversas_ComMensagem()
    {
        var repo = _mongo.NewRepository();
        var alice = User("alice");

        await repo.GetOrCreateDirectAsync(alice, User("bob"));

        var ativa = await repo.GetOrCreateDirectAsync(alice, User("carol"));
        await repo.AddMessageAsync(new Message
        {
            ConversationId = ativa.Id,
            SenderId = alice.UserId,
            Content = "oi",
            SentAt = DateTime.UtcNow,
        });

        var list = await repo.GetConversationsForUserAsync(alice.UserId);

        var only = Assert.Single(list);
        Assert.Equal(ativa.Id, only.Id);
    }
}
