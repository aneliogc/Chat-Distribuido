using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace IntegrationTests;

public record TestUser(string UserId, string Username, string Token);

public record ConversationDto(
    string Id,
    string Type,
    string? Name,
    List<ParticipantDto> Participants,
    DateTime? LastMessageAt);

public record ParticipantDto(string UserId, string Username);

public record MessageDto(
    string Id,
    string ConversationId,
    string SenderId,
    string SenderUsername,
    string Content,
    DateTime SentAt);

public static class TestClient
{
    public static string AuthBaseUrl =>
        Environment.GetEnvironmentVariable("AUTH_BASE_URL") ?? "http://authservice:8080";

    public static string ChatBaseUrl =>
        Environment.GetEnvironmentVariable("CHAT_BASE_URL") ?? "http://nginx:80";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly HttpClient Http = new();

    public static async Task<TestUser> RegisterAndLoginAsync()
    {
        var username = "itest_" + Guid.NewGuid().ToString("N")[..12];
        var password = "senha123";
        var email = username + "@test.com";

        var register = await Http.PostAsJsonAsync($"{AuthBaseUrl}/api/auth/register",
            new { username, email, password });
        register.EnsureSuccessStatusCode();

        var login = await Http.PostAsJsonAsync($"{AuthBaseUrl}/api/auth/login",
            new { usernameOrEmail = username, password });
        login.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        return new TestUser(
            root.GetProperty("userId").GetString()!,
            root.GetProperty("username").GetString()!,
            root.GetProperty("token").GetString()!);
    }

    public static async Task<HttpResponseMessage> GetConversationsAsync(string? token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"{ChatBaseUrl}/api/conversations");
        if (token is not null)
            req.Headers.Add("Authorization", $"Bearer {token}");
        return await Http.SendAsync(req);
    }

    public static async Task<List<MessageDto>> GetMessagesAsync(string token, string conversationId)
    {
        var req = new HttpRequestMessage(HttpMethod.Get,
            $"{ChatBaseUrl}/api/conversations/{conversationId}/messages");
        req.Headers.Add("Authorization", $"Bearer {token}");
        var res = await Http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<MessageDto>>(
            await res.Content.ReadAsStringAsync(), Json)!;
    }

    public static async Task<string> GetHealthInstanceAsync()
    {
        using var doc = JsonDocument.Parse(await Http.GetStringAsync($"{ChatBaseUrl}/health"));
        return doc.RootElement.GetProperty("instance").GetString()!;
    }

    public static async Task<HubConnection> ConnectHubAsync(string token)
    {
        var conn = new HubConnectionBuilder()
            .WithUrl($"{ChatBaseUrl}/hub/chat", options =>
                options.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .Build();

        await conn.StartAsync();
        return conn;
    }
}
