using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class LoadTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    private static int UserCount =>
        int.TryParse(Environment.GetEnvironmentVariable("LOAD_USERS"), out var n) && n >= 10 ? n : 12;

    private readonly ITestOutputHelper _output;

    public LoadTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task MultiplosUsuarios_LoginSimultaneo_DistribuidoEntreReplicas()
    {
        var n = UserCount;

        var users = await Task.WhenAll(Enumerable.Range(0, n)
            .Select(_ => TestClient.RegisterAndLoginAsync()));

        Assert.All(users, u => Assert.False(string.IsNullOrWhiteSpace(u.Token)));
        Assert.Equal(n, users.Select(u => u.UserId).Distinct().Count());

        var instances = new ConcurrentBag<string>();
        await Task.WhenAll(Enumerable.Range(0, n * 3)
            .Select(async _ => instances.Add(await TestClient.GetHealthInstanceAsync())));

        var replicas = instances.Distinct().OrderBy(x => x).ToList();
        _output.WriteLine($"{n} usuarios logados. Replicas que responderam: {string.Join(", ", replicas)}");

        Assert.True(replicas.Count > 1,
            $"Esperava balanceamento entre varias replicas; obtive: {string.Join(", ", replicas)}");
    }

    [Fact]
    public async Task MultiplosUsuarios_TrocaDeMensagensSimultanea_TodasEntreguesEPersistidas()
    {
        var n = UserCount;

        var users = await Task.WhenAll(Enumerable.Range(0, n)
            .Select(_ => TestClient.RegisterAndLoginAsync()));

        var hubs = await Task.WhenAll(users.Select(u => TestClient.ConnectHubAsync(u.Token)));

        try
        {
            var participants = users.Skip(1)
                .Select(u => new ParticipantDto(u.UserId, u.Username))
                .ToList();

            var group = await hubs[0].InvokeAsync<ConversationDto>(
                "CreateGroup", "Carga SD", participants);

            var counters = new int[n];
            var done = Enumerable.Range(0, n)
                .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
                .ToArray();

            for (var i = 0; i < n; i++)
            {
                var idx = i;
                hubs[idx].On<MessageDto>("ReceiveMessage", msg =>
                {
                    if (msg.ConversationId != group.Id) return;
                    if (Interlocked.Increment(ref counters[idx]) >= n)
                        done[idx].TrySetResult();
                });
            }

            await Task.WhenAll(Enumerable.Range(0, n).Select(i =>
                hubs[i].InvokeAsync<MessageDto>("SendMessage", group.Id, $"msg do usuario {i}")));

            await WaitAsync(Task.WhenAll(done.Select(d => d.Task)));

            Assert.All(counters, c => Assert.Equal(n, c));

            var history = await TestClient.GetMessagesAsync(users[0].Token, group.Id);
            Assert.Equal(n, history.Count);

            _output.WriteLine(
                $"{n} usuarios trocaram mensagens simultaneamente: {n * n} entregas em tempo real, {history.Count} mensagens persistidas.");
        }
        finally
        {
            await Task.WhenAll(hubs.Select(h => h.DisposeAsync().AsTask()));
        }
    }

    private static async Task WaitAsync(Task task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(Timeout));
        if (completed != task)
            throw new TimeoutException("Nem todas as mensagens chegaram dentro do tempo limite.");
        await task;
    }
}
