using CoreMs.Common.Exceptions;
using CoreMs.Common.Repository;
using CoreMs.DocumentMs.Core.Models;
using CoreMs.DocumentMs.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMs.DocumentMs.Api.Controllers;

/// <summary>
/// Document collection operations — upload and list.
/// </summary>
[ApiController]
[Route("api/documents")]
[Authorize]
[Produces("application/json")]
public class DocumentsListController(DocumentService documentService) : ControllerBase
{
    /// <summary>
    /// Upload a file via multipart form data.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentDto>> Upload(
        IFormFile file,
        [FromForm] string? name,
        [FromForm] string? description,
        [FromForm] string? visibility,
        [FromForm] string? tags,
        [FromForm] bool replace = false,
        CancellationToken ct = default)
    {
        var visibilityEnum = visibility is not null
            ? Enum.Parse<Core.Enums.DocumentVisibility>(visibility, ignoreCase: true)
            : (Core.Enums.DocumentVisibility?)null;

        var tagsList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var request = new UploadDocumentRequest(name, description, visibilityEnum, tagsList, replace);

        await using var stream = file.OpenReadStream();
        var result = await documentService.UploadAsync(stream, file.FileName, file.Length, file.ContentType, request, ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Upload a file via Base64 encoded content.
    /// </summary>
    [HttpPost("upload/base64")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentDto>> UploadBase64([FromBody] UploadBase64Request request, CancellationToken ct)
    {
        var result = await documentService.UploadBase64Async(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Get paginated list of documents.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DocumentDto>>> ListDocuments([FromQuery] QueryParameters parameters, CancellationToken ct)
    {
        var result = await documentService.ListDocumentsAsync(parameters, ct);
        return Ok(result);
    }
}
