using CoreMs.TemplateMs.Core.Models;
using CoreMs.TemplateMs.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.TemplateMs.Api.Controllers;

/// <summary>
/// Endpoints for template rendering and metadata retrieval.
/// Available to any authenticated user.
/// </summary>
[ApiController]
[Route("api/templates")]
[Authorize]
[Produces("application/json")]
public class TemplateRenderController(TemplateService templateService) : ControllerBase
{
    /// <summary>
    /// Render a template with parameter substitution.
    /// </summary>
    [HttpPost("render")]
    public async Task<ActionResult<RenderTemplateResponse>> Render(
        [FromBody] RenderTemplateRequest request, CancellationToken ct)
    {
        var result = await templateService.RenderAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get template metadata (without content) for a specific templateId and language.
    /// </summary>
    [HttpGet("{templateId}/{language}/metadata")]
    public async Task<ActionResult<TemplateMetadataDto>> GetMetadata(
        string templateId, string language, CancellationToken ct)
    {
        var result = await templateService.GetMetadataAsync(templateId, language, ct);
        return Ok(result);
    }
}
