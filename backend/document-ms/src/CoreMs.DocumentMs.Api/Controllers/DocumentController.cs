using CoreMs.Common.Exceptions;
using CoreMs.DocumentMs.Core.Models;
using CoreMs.DocumentMs.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.DocumentMs.Api.Controllers;

/// <summary>
/// Document operations for a specific document by UUID.
/// </summary>
[ApiController]
[Route("api/documents/{uuid:guid}")]
[Authorize]
[Produces("application/json")]
public class DocumentController(DocumentService documentService) : ControllerBase
{
    /// <summary>
    /// Get document metadata by UUID.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetDocument(Guid uuid, CancellationToken ct)
    {
        var result = await documentService.GetDocumentAsync(uuid, ct);
        return Ok(result);
    }

    /// <summary>
    /// Update document metadata.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> UpdateDocument(Guid uuid, [FromBody] UpdateDocumentRequest request, CancellationToken ct)
    {
        var result = await documentService.UpdateDocumentAsync(uuid, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Delete a document (soft or hard).
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid uuid, [FromQuery] bool permanent = false, CancellationToken ct = default)
    {
        await documentService.DeleteDocumentAsync(uuid, permanent, ct);
        return NoContent();
    }

    /// <summary>
    /// View document inline (Content-Disposition: inline).
    /// </summary>
    [HttpGet("view")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ViewDocument(Guid uuid, CancellationToken ct)
    {
        var (stream, contentType, filename) = await documentService.StreamDocumentAsync(uuid, ct);
        Response.Headers.ContentDisposition = $"inline; filename=\"{filename}\"";
        return File(stream, contentType);
    }

    /// <summary>
    /// Download document as attachment.
    /// </summary>
    [HttpGet("download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(Guid uuid, CancellationToken ct)
    {
        var (stream, contentType, filename) = await documentService.StreamDocumentAsync(uuid, ct);
        return File(stream, contentType, filename);
    }

    /// <summary>
    /// Generate an access link for a BY_LINK document.
    /// </summary>
    [HttpPost("link")]
    [ProducesResponseType(typeof(DocumentLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentLinkDto>> GenerateAccessLink(Guid uuid, [FromBody] GenerateLinkRequest request, CancellationToken ct)
    {
        var result = await documentService.GenerateAccessLinkAsync(uuid, request, ct);
        return Ok(result);
    }
}
