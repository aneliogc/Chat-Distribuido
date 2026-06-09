using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "chat",
        // util na Parte 3 para ver QUAL replica respondeu (balanceamento).
        instance = Environment.MachineName,
        utc = DateTime.UtcNow
    });
}
