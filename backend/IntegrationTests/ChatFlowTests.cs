using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace IntegrationTests;

public class ChatFlowTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Auth_RegisterThenLogin_ReturnsToken()
    {
        var user = await TestClient.RegisterAndLoginAsync();

        Assert.False(string.IsNullOrWhiteSpace(user.Token));
        Assert.False(string.IsNullOrWhiteSpace(user.UserId));
    }

    [Fact]
    public async Task Chat_SemToken_Retorna401()
    {
        var res = await TestClient.GetConversationsAsync(token: null);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task FluxoCompleto_EnviarMensagem1a1_EntregueEmTempoReal_E_Persistida()
    {
        var alice = await TestClient.RegisterAndLoginAsync();
        var bob = await TestClient.RegisterAndLoginAsync();

        await using var aliceHub = await TestClient.ConnectHubAsync(alice.Token);
        await using var bobHub = await TestClient.ConnectHubAsync(bob.Token);

        var received = new TaskCompletionSource<MessageDto>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        bobHub.On<MessageDto>("ReceiveMessage", msg => received.TrySetResult(msg));

        var conv = await aliceHub.InvokeAsync<ConversationDto>(
            "StartDirect", bob.UserId, bob.Username);

        var sent = await aliceHub.InvokeAsync<MessageDto>(
            "SendMessage", conv.Id, "Ola, Bob!");

        var delivered = await WaitAsync(received.Task);

        Assert.Equal("Ola, Bob!", delivered.Content);
        Assert.Equal(alice.UserId, delivered.SenderId);
        Assert.Equal(sent.Id, delivered.Id);

        var history = await TestClient.GetMessagesAsync(bob.Token, conv.Id);
        Assert.Contains(history, m => m.Id == sent.Id && m.Content == "Ola, Bob!");
    }

    [Fact]
    public async Task MensagemEmGrupo_1aN_EntregueATodosOsParticipantes()
    {
        var alice = await TestClient.RegisterAndLoginAsync();
        var bob = await TestClient.RegisterAndLoginAsync();
        var carol = await TestClient.RegisterAndLoginAsync();

        await using var aliceHub = await TestClient.ConnectHubAsync(alice.Token);
        await using var bobHub = await TestClient.ConnectHubAsync(bob.Token);
        await using var carolHub = await TestClient.ConnectHubAsync(carol.Token);

        var bobGot = new TaskCompletionSource<MessageDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var carolGot = new TaskCompletionSource<MessageDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        bobHub.On<MessageDto>("ReceiveMessage", m => bobGot.TrySetResult(m));
        carolHub.On<MessageDto>("ReceiveMessage", m => carolGot.TrySetResult(m));

        var participants = new[]
        {
            new ParticipantDto(bob.UserId, bob.Username),
            new ParticipantDto(carol.UserId, carol.Username),
        };
        var group = await aliceHub.InvokeAsync<ConversationDto>(
            "CreateGroup", "Trabalho SD", participants);

        await aliceHub.InvokeAsync<MessageDto>("SendMessage", group.Id, "Ola, grupo!");

        var bobMsg = await WaitAsync(bobGot.Task);
        var carolMsg = await WaitAsync(carolGot.Task);

        Assert.Equal("Ola, grupo!", bobMsg.Content);
        Assert.Equal("Ola, grupo!", carolMsg.Content);
    }

    [Fact]
    public async Task Balanceamento_RequisicoesRest_DistribuidasEntreReplicas()
    {
        var instances = new HashSet<string>();
        for (var i = 0; i < 30; i++)
            instances.Add(await TestClient.GetHealthInstanceAsync());

        Assert.True(instances.Count > 1,
            $"Esperava respostas de mais de uma replica; obtive: {string.Join(", ", instances)}");
    }

    private static async Task<T> WaitAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(Timeout));
        if (completed != task)
            throw new TimeoutException("Evento em tempo real nao chegou dentro do tempo limite.");
        return await task;
    }
}
