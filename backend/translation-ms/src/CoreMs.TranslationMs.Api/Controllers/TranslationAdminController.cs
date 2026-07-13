using CoreMs.Common.Exceptions;
using CoreMs.Common.Repository;
using CoreMs.Common.Security;
using CoreMs.TranslationMs.Core.Models;
using CoreMs.TranslationMs.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.TranslationMs.Api.Controllers;

[ApiController]
[Route("api/admin/translation")]
[Authorize(Roles = CoreMsRoles.TranslationMsAdmin)]
[Produces("application/json")]
public class TranslationAdminController(TranslationService translationService) : ControllerBase
{
    /// <summary>
    /// Get translation bundle with metadata (admin).
    /// </summary>
    [HttpGet("{realm}/{lang}")]
    [ProducesResponseType(typeof(TranslationBundleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TranslationBundleDto>> GetAdminTranslation(
        string realm, string lang, CancellationToken ct)
    {
        var result = await translationService.GetAdminTranslationAsync(realm, lang, ct);
        return Ok(result);
    }

    /// <summary>
    /// List realms with their languages (paginated).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RealmLanguagesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RealmLanguagesDto>>> ListRealms(
        [FromQuery] QueryParameters parameters, CancellationToken ct)
    {
        var result = await translationService.ListRealmsAsync(parameters, ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a new translation bundle.
    /// </summary>
    [HttpPost("{realm}/{lang}")]
    [ProducesResponseType(typeof(TranslationBundleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TranslationBundleDto>> CreateTranslation(
        string realm, string lang, [FromBody] TranslationRequest request, CancellationToken ct)
    {
        var result = await translationService.CreateTranslationAsync(realm, lang, request, ct);
        return CreatedAtAction(nameof(GetAdminTranslation), new { realm, lang }, result);
    }

    /// <summary>
    /// Update an existing translation bundle.
    /// </summary>
    [HttpPut("{realm}/{lang}")]
    [ProducesResponseType(typeof(TranslationBundleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TranslationBundleDto>> UpdateTranslation(
        string realm, string lang, [FromBody] TranslationRequest request, CancellationToken ct)
    {
        var result = await translationService.UpdateTranslationAsync(realm, lang, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Delete a translation bundle.
    /// </summary>
    [HttpDelete("{realm}/{lang}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTranslation(
        string realm, string lang, CancellationToken ct)
    {
        await translationService.DeleteTranslationAsync(realm, lang, ct);
        return NoContent();
    }
}
