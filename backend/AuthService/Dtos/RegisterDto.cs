using System.ComponentModel.DataAnnotations;

namespace AuthService.Dtos;

public record RegisterDto(
    [Required, MinLength(3), MaxLength(40)] string Username,
    [Required, EmailAddress, MaxLength(120)] string Email,
    [Required, MinLength(6), MaxLength(100)] string Password
);
