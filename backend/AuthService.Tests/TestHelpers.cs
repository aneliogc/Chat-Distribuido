using AuthService.Data;
using AuthService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuthService.Tests;

internal static class TestHelpers
{
    public static AuthDbContext NewDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(options);
    }

    public static IConfiguration JwtConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "sd-chat-auth-tests",
                ["Jwt:Audience"] = "sd-chat-clients-tests",
                ["Jwt:Secret"] = "test-secret-key-with-at-least-32-characters!!",
                ["Jwt:ExpiresInMinutes"] = "60",
            })
            .Build();

    public static AuthServiceImpl NewAuthService(AuthDbContext db) =>
        new(db, new JwtTokenService(JwtConfig()));
}
