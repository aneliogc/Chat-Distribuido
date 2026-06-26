using ChatService.Config;
using ChatService.Data;
using EphemeralMongo;
using Microsoft.Extensions.Options;

namespace ChatService.Tests;

public sealed class MongoFixture : IDisposable
{
    private readonly IMongoRunner _runner;

    public MongoFixture()
    {
        _runner = MongoRunner.Run();
    }

    public ChatRepository NewRepository()
    {
        var settings = Options.Create(new MongoSettings
        {
            ConnectionString = _runner.ConnectionString,
            Database = "chat_test_" + Guid.NewGuid().ToString("N"),
        });
        return new ChatRepository(new MongoContext(settings));
    }

    public void Dispose() => _runner.Dispose();
}
