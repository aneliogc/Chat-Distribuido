using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Models;
using AuthService.Services;
using Xunit;

namespace AuthService.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void Generate_DeveProduzirToken_ComAsClaimsDoUsuario()
    {
        var jwt = new JwtTokenService(TestHelpers.JwtConfig());
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Email = "alice@email.com",
            PasswordHash = "irrelevante"
        };

        var (token, expiresAt) = jwt.Generate(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(user.Id.ToString(), parsed.Subject);
        Assert.Equal("alice", parsed.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("alice@email.com", parsed.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("sd-chat-auth-tests", parsed.Issuer);
        Assert.Contains("sd-chat-clients-tests", parsed.Audiences);
    }

    [Fact]
    public void Generate_DeveGerarJti_DiferentePorToken()
    {
        var jwt = new JwtTokenService(TestHelpers.JwtConfig());
        var user = new User { Id = Guid.NewGuid(), Username = "bob", Email = "bob@email.com" };

        var (t1, _) = jwt.Generate(user);
        var (t2, _) = jwt.Generate(user);

        string Jti(string t) => new JwtSecurityTokenHandler().ReadJwtToken(t)
            .Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(Jti(t1), Jti(t2));
    }
}
