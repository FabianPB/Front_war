using Microsoft.AspNetCore.Mvc;
using War.Api.Application.SkillAdmin;
using War.Core.Skills;

namespace War.Api.Controllers;

[ApiController]
[Route("api/admin/skills")]
public sealed class SkillAdminController : ControllerBase
{
    private readonly ISkillAdminCatalogService _catalogService;
    private readonly ISkillAdminOptionsService _optionsService;

    public SkillAdminController(
        ISkillAdminCatalogService catalogService,
        ISkillAdminOptionsService optionsService)
    {
        _catalogService = catalogService;
        _optionsService = optionsService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<SkillAdminOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.GetOverviewAsync(cancellationToken));
    }

    [HttpGet("options")]
    public ActionResult<SkillAdminOptionsDto> GetOptions()
    {
        return Ok(_optionsService.GetOptions());
    }

    [HttpGet("{recordId:guid}")]
    public async Task<ActionResult<SkillAdminDetailDto>> GetDetail(Guid recordId, CancellationToken cancellationToken)
    {
        var detail = await _catalogService.GetDetailAsync(recordId, cancellationToken);
        return detail is null
            ? NotFound(new { message = $"Admin skill record '{recordId}' was not found." })
            : Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<SkillAdminDetailDto>> Create([FromBody] SkillAdminUpsertDocumentDto request, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await _catalogService.CreateAsync(ToUpsertRequest(request), cancellationToken);
            return CreatedAtAction(nameof(GetDetail), new { recordId = detail.RecordId }, detail);
        }
        catch (SkillAdminRequestException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPut("{recordId:guid}")]
    public async Task<ActionResult<SkillAdminDetailDto>> Update(Guid recordId, [FromBody] SkillAdminUpsertDocumentDto request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _catalogService.UpdateAsync(recordId, ToUpsertRequest(request), cancellationToken));
        }
        catch (SkillAdminRequestException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("{recordId:guid}/publish")]
    public async Task<ActionResult<SkillAdminStateChangeResultDto>> Publish(Guid recordId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _catalogService.PublishAsync(recordId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("{recordId:guid}/unpublish")]
    public async Task<ActionResult<SkillAdminStateChangeResultDto>> Unpublish(Guid recordId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _catalogService.UnpublishAsync(recordId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("{recordId:guid}/archive")]
    public async Task<ActionResult<SkillAdminStateChangeResultDto>> Archive(Guid recordId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _catalogService.ArchiveAsync(recordId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpDelete("{recordId:guid}")]
    public Task<ActionResult<SkillAdminStateChangeResultDto>> Delete(Guid recordId, CancellationToken cancellationToken)
    {
        return Archive(recordId, cancellationToken);
    }

    [HttpPost("preview")]
    public async Task<ActionResult<SkillAdminPreviewDto>> Preview([FromBody] SkillAdminUpsertDocumentDto request, [FromQuery] Guid? currentRecordId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _catalogService.PreviewAsync(ToUpsertRequest(request), currentRecordId, cancellationToken));
        }
        catch (SkillAdminRequestException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("compare")]
    public async Task<ActionResult<SkillAdminComparisonDto>> Compare([FromQuery] Guid leftRecordId, [FromQuery] Guid rightRecordId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _catalogService.CompareAsync(leftRecordId, rightRecordId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    private static SkillAdminUpsertRequest ToUpsertRequest(SkillAdminUpsertDocumentDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        SkillDefinition definition;
        try
        {
            definition = SkillAdminJsonSerializer.DeserializeSkillDefinition(request.Definition);
        }
        catch (Exception exception)
        {
            throw new SkillAdminRequestException("The admin skill payload could not be parsed into a valid SkillDefinition.", exception);
        }

        return new SkillAdminUpsertRequest(definition);
    }

    private sealed class SkillAdminRequestException : Exception
    {
        public SkillAdminRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
