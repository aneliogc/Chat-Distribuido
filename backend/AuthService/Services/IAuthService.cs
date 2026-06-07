using AuthService.Dtos;

namespace AuthService.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<UserSummaryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<UserSummaryDto>> ListAsync(Guid? excludeId, CancellationToken ct = default);
}
