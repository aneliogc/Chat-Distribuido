namespace AuthService.Dtos;

public record AuthResponseDto(
    Guid UserId,
    string Username,
    string Email,
    string Token,
    DateTime ExpiresAt
);

public record UserSummaryDto(
    Guid Id,
    string Username,
    string Email
);
