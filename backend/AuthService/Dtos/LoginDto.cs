using System.ComponentModel.DataAnnotations;

namespace AuthService.Dtos;

public record LoginDto(
    [Required] string UsernameOrEmail,
    [Required] string Password
);
