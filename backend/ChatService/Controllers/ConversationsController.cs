using System.Security.Claims;
using ChatService.Data;
using ChatService.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers;

/// <summary>
/// Consultas de historico (REST). Tempo real fica no ChatHub;
/// aqui sao buscas pontuais: listar conversas e carregar mensagens antigas.
/// </summary>
[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IChatRepository _repo;

    public ConversationsController(IChatRepository repo)
    {
        _repo = repo;
    }

    private Guid CurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    /// <summary>Lista as conversas do usuario logado (mais recentes primeiro).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> GetMyConversations(CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var conversations = await _repo.GetConversationsForUserAsync(userId, ct);
        return Ok(conversations.Select(c => c.ToDto()).ToList());
    }

    /// <summary>Carrega o historico de mensagens de uma conversa.</summary>
    [HttpGet("{id}/messages")]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> GetMessages(
        string id, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var userId = CurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        // Autorizacao: so quem participa pode ler o historico.
        if (!await _repo.IsParticipantAsync(id, userId, ct))
            return Forbid();

        var messages = await _repo.GetMessagesAsync(id, limit, ct);
        return Ok(messages.Select(m => m.ToDto()).ToList());
    }
}
