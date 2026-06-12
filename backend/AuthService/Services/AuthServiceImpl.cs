using AuthService.Data;
using AuthService.Dtos;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public class AuthServiceImpl : IAuthService
{
    private readonly AuthDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthServiceImpl(AuthDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<UserSummaryDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var username = dto.Username.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Username == username || u.Email == email, ct);
        if (exists)
            throw new InvalidOperationException("Username or email already taken.");

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new UserSummaryDto(user.Id, user.Username, user.Email);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var key = dto.UsernameOrEmail.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Username == dto.UsernameOrEmail.Trim() || u.Email == key, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Usuário ou senha inválidos.");

        var (token, expiresAt) = _jwt.Generate(user);
        return new AuthResponseDto(user.Id, user.Username, user.Email, token, expiresAt);
    }

    public async Task<UserSummaryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, ct);
        return user is null ? null : new UserSummaryDto(user.Id, user.Username, user.Email);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> ListAsync(Guid? excludeId, CancellationToken ct = default)
    {
        var q = _db.Users.AsNoTracking().AsQueryable();
        if (excludeId is { } id) q = q.Where(u => u.Id != id);
        return await q
            .OrderBy(u => u.Username)
            .Select(u => new UserSummaryDto(u.Id, u.Username, u.Email))
            .ToListAsync(ct);
    }
}
