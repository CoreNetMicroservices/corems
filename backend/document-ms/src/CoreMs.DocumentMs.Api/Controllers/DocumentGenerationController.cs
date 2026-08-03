using CoreMs.DocumentMs.Core.Models;
using CoreMs.DocumentMs.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.DocumentMs.Api.Controllers;

/// <summary>
/// Generate documents from templates.
/// </summary>
[ApiController]
[Route("api/documents/generate")]
[Authorize]
[Produces("application/json")]
public class DocumentGenerationController(DocumentGenerationService generationService) : ControllerBase
{
    /// <summary>
    /// Render a template and save the result as a document in the user's storage.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DocumentDto>> GenerateAndSave(
        [FromBody] GenerateDocumentRequest request, CancellationToken ct)
    {
        var result = await generationService.GenerateAndSaveAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Render a template and stream the result directly (no storage).
    /// </summary>
    [HttpPost("stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateAndStream(
        [FromBody] GenerateDocumentRequest request, CancellationToken ct)
    {
        var (stream, contentType, fileName) = await generationService.GenerateAndStreamAsync(request, ct);
        return File(stream, contentType, fileName);
    }
}
