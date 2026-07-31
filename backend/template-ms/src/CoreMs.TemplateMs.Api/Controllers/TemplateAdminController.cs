using CoreMs.Common.Repository;
using CoreMs.Common.Security;
using CoreMs.TemplateMs.Core.Models;
using CoreMs.TemplateMs.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.TemplateMs.Api.Controllers;

/// <summary>
/// Admin endpoints for template CRUD operations.
/// </summary>
[ApiController]
[Route("api/templates")]
[Authorize(Roles = CoreMsRoles.TemplateMsAdmin)]
[Produces("application/json")]
public class TemplateAdminController(TemplateService templateService) : ControllerBase
{
    /// <summary>
    /// Get a paginated list of templates.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TemplateDto>>> GetAll(
        [FromQuery] QueryParameters parameters, CancellationToken ct)
    {
        var result = await templateService.GetAllAsync(parameters, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get a template by UUID.
    /// </summary>
    [HttpGet("{templateUuid:guid}")]
    public async Task<ActionResult<TemplateDto>> GetByUuid(Guid templateUuid, CancellationToken ct)
    {
        var result = await templateService.GetByUuidAsync(templateUuid, ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a new template.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TemplateDto>> Create(
        [FromBody] CreateTemplateRequest request, CancellationToken ct)
    {
        var result = await templateService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetByUuid), new { templateUuid = result.Id }, result);
    }

    /// <summary>
    /// Update an existing template.
    /// </summary>
    [HttpPut("{templateUuid:guid}")]
    public async Task<ActionResult<TemplateDto>> Update(
        Guid templateUuid, [FromBody] UpdateTemplateRequest request, CancellationToken ct)
    {
        var result = await templateService.UpdateAsync(templateUuid, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Soft-delete a template.
    /// </summary>
    [HttpDelete("{templateUuid:guid}")]
    public async Task<IActionResult> Delete(Guid templateUuid, CancellationToken ct)
    {
        await templateService.DeleteAsync(templateUuid, ct);
        return NoContent();
    }
}
