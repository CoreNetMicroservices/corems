using CoreMs.Common.Exceptions;
using CoreMs.TranslationMs.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.TranslationMs.Api.Controllers;

[ApiController]
[Produces("application/json")]
public class TranslationController(TranslationService translationService) : ControllerBase
{
    /// <summary>
    /// Get translation data by realm and language.
    /// </summary>
    [HttpGet("api/translation/{realm}/{lang}")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Dictionary<string, string>>> GetTranslations(
        string realm, string lang, CancellationToken ct)
    {
        var data = await translationService.GetTranslationsAsync(realm, lang, ct);
        return Ok(data);
    }

    /// <summary>
    /// Get available languages for a realm.
    /// </summary>
    [HttpGet("api/languages/{realm}")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetLanguages(string realm, CancellationToken ct)
    {
        var languages = await translationService.GetLanguagesByRealmAsync(realm, ct);
        return Ok(languages);
    }
}
