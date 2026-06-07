using System.Security.Claims;
using AuthService.Dtos;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserSummaryDto>> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        try
        {
            var res = await _auth.RegisterAsync(dto, ct);
            return Created($"/api/auth/users/{res.Id}", res);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        try
        {
            var res = await _auth.LoginAsync(dto, ct);
            return Ok(res);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserSummaryDto>> Me(CancellationToken ct)
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(idStr, out var id)) return Unauthorized();
        var user = await _auth.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [Authorize]
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> ListUsers(CancellationToken ct)
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        Guid? exclude = Guid.TryParse(idStr, out var id) ? id : null;
        return Ok(await _auth.ListAsync(exclude, ct));
    }
}
