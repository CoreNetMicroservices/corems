using CoreMs.Common.Exceptions;
using CoreMs.DocumentMs.Core.Models;
using CoreMs.DocumentMs.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.DocumentMs.Api.Controllers;

/// <summary>
/// Public document access — no authentication required.
/// </summary>
[ApiController]
[Route("api/public/documents")]
[Produces("application/json")]
public class PublicDocumentsController(PublicDocumentService publicDocumentService) : ControllerBase
{
    /// <summary>
    /// Get public document metadata.
    /// </summary>
    [HttpGet("{uuid:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetPublicDocument(Guid uuid, CancellationToken ct)
    {
        var result = await publicDocumentService.GetPublicDocumentAsync(uuid, ct);
        return Ok(result);
    }

    /// <summary>
    /// View public document inline.
    /// </summary>
    [HttpGet("{uuid:guid}/view")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ViewPublicDocument(Guid uuid, CancellationToken ct)
    {
        var (stream, contentType, filename) = await publicDocumentService.StreamPublicDocumentAsync(uuid, ct);
        Response.Headers.ContentDisposition = $"inline; filename=\"{filename}\"";
        return File(stream, contentType);
    }

    /// <summary>
    /// Download public document as attachment.
    /// </summary>
    [HttpGet("{uuid:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPublicDocument(Guid uuid, CancellationToken ct)
    {
        var (stream, contentType, filename) = await publicDocumentService.StreamPublicDocumentAsync(uuid, ct);
        return File(stream, contentType, filename);
    }

    /// <summary>
    /// Access a document's metadata by access token (link).
    /// </summary>
    [HttpGet("link/{token}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DocumentDto>> GetDocumentByToken(string token, CancellationToken ct)
    {
        var result = await publicDocumentService.GetDocumentByTokenAsync(token, ct);
        return Ok(result);
    }

    /// <summary>
    /// View a document inline by access token (link).
    /// </summary>
    [HttpGet("link/{token}/view")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ViewDocumentByToken(string token, CancellationToken ct)
    {
        var (stream, contentType, filename) = await publicDocumentService.StreamDocumentByTokenAsync(token, ct);
        Response.Headers.ContentDisposition = $"inline; filename=\"{filename}\"";
        return File(stream, contentType);
    }

    /// <summary>
    /// Download a document by access token (link).
    /// </summary>
    [HttpGet("link/{token}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadDocumentByToken(string token, CancellationToken ct)
    {
        var (stream, contentType, filename) = await publicDocumentService.StreamDocumentByTokenAsync(token, ct);
        return File(stream, contentType, filename);
    }
}
