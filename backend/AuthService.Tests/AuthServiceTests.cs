using AuthService.Dtos;
using AuthService.Models;
using Xunit;

namespace AuthService.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_DeveCriarUsuario_E_GuardarSenhaComHash()
    {
        using var db = TestHelpers.NewDbContext();
        var auth = TestHelpers.NewAuthService(db);

        var result = await auth.RegisterAsync(new RegisterDto("alice", "Alice@Email.com", "senha123"));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("alice", result.Username);
        Assert.Equal("alice@email.com", result.Email);

        var saved = Assert.Single(db.Users);
        Assert.NotEqual("senha123", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("senha123", saved.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_ComUsernameDuplicado_DeveLancarExcecao()
    {
        using var db = TestHelpers.NewDbContext();
        var auth = TestHelpers.NewAuthService(db);

        await auth.RegisterAsync(new RegisterDto("bob", "bob@email.com", "senha123"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            auth.RegisterAsync(new RegisterDto("bob", "outro@email.com", "senha123")));
    }

    [Fact]
    public async Task RegisterAsync_ComEmailDuplicado_DeveLancarExcecao()
    {
        using var db = TestHelpers.NewDbContext();
        var auth = TestHelpers.NewAuthService(db);

        await auth.RegisterAsync(new RegisterDto("carol", "carol@email.com", "senha123"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            auth.RegisterAsync(new RegisterDto("carol2", "CAROL@email.com", "senha123")));
    }

    [Fact]
    public async Task LoginAsync_ComCredenciaisValidas_DeveRetornarToken()
    {
        using var db = TestHelpers.NewDbContext();
        var auth = TestHelpers.NewAuthService(db);
        await auth.RegisterAsync(new RegisterDto("dave", "dave@email.com", "senha123"));

        var res = await auth.LoginAsync(new LoginDto("dave", "senha123"));

        Assert.Equal("dave", res.Username);
        Assert.False(string.IsNullOrWhiteSpace(res.Token));
        Assert.True(res.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_PodeUsarEmail_ComoIdentificador()
    {
        using var db = TestHelpers.NewDbContext();
        var auth = TestHelpers.NewAuthService(db);
        await auth.RegisterAsync(new RegisterDto("erin", "erin@email.com", "senha123"));

        var res = await auth.LoginAsync(new LoginDto("ERIN@email.com", "senha123"));

        Assert.Equal("erin", res.Username);
    }

    [Fact]
    public async Task LoginAsync_ComSenhaErrada_DeveLancarUnauthorized()
    {
        using var db = TestHelpers.NewDbContext();
        var auth = TestHelpers.NewAuthService(db);
        await auth.RegisterAsync(new RegisterDto("frank", "frank@email.com", "senha123"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.LoginAsync(new LoginDto("frank", "senhaErrada")));
    }

    [Fact]
    public async Task LoginAsync_ComUsuarioInexistente_DeveLancarUnauthorized()
    {
        using var db = TestHelpers.NewDbContext();
        var auth = TestHelpers.NewAuthService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.LoginAsync(new LoginDto("ninguem", "senha123")));
    }

    [Fact]
    public async Task ListAsync_DeveExcluir_OUsuarioInformado()
    {
        using var db = TestHelpers.NewDbContext();
        var auth = TestHelpers.NewAuthService(db);
        var a = await auth.RegisterAsync(new RegisterDto("gina", "gina@email.com", "senha123"));
        await auth.RegisterAsync(new RegisterDto("hugo", "hugo@email.com", "senha123"));

        var list = await auth.ListAsync(excludeId: a.Id);

        Assert.Single(list);
        Assert.Equal("hugo", list[0].Username);
    }
}
