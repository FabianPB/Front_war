using Microsoft.AspNetCore.Mvc;
using War.Api.Application.Characters;

namespace War.Api.Controllers;

/// <summary>
/// Endpoints REST de consulta para personajes persistidos.
/// </summary>
[ApiController]
[Route("api/characters")]
public sealed class CharactersController : ControllerBase
{
    private readonly ICharacterSnapshotQueryService _snapshotQueryService;

    public CharactersController(ICharacterSnapshotQueryService snapshotQueryService)
    {
        _snapshotQueryService = snapshotQueryService;
    }

    /// <summary>
    /// Snapshot completo de un personaje: stats finales, progresión, recursos y power score.
    /// </summary>
    [HttpGet("{id:guid}/snapshot")]
    public async Task<ActionResult<CharacterSnapshotDto>> GetSnapshot(Guid id, CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotQueryService.GetSnapshotAsync(id, cancellationToken);

        return snapshot is null
            ? NotFound(new { message = $"Character '{id}' was not found." })
            : Ok(snapshot);
    }
}
