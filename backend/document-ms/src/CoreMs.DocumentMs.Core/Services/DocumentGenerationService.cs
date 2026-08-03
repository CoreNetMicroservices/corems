using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.DocumentMs.Core.Enums;
using CoreMs.DocumentMs.Core.Exceptions;
using CoreMs.DocumentMs.Core.Models;
using CoreMs.TemplateMs.Client;

namespace CoreMs.DocumentMs.Core.Services;

/// <summary>
/// Generates PDF documents from templates. Either saves to storage or streams the result.
/// </summary>
[Service]
public class DocumentGenerationService(
    TemplateMsClient templateClient,
    PdfRenderingService pdfRenderer,
    DocumentService documentService)
{
    /// <summary>
    /// Render a template to PDF and save it as a document in the user's storage.
    /// </summary>
    public async Task<DocumentDto> GenerateAndSaveAsync(GenerateDocumentRequest request, CancellationToken ct = default)
    {
        var pdfBytes = await RenderToPdfAsync(request, ct);

        var fileName = ResolveFileName(request, ".pdf");
        using var stream = new MemoryStream(pdfBytes);

        var uploadRequest = new UploadDocumentRequest(
            Name: Path.GetFileNameWithoutExtension(fileName),
            Description: request.Description ?? $"Generated from template: {request.TemplateId}",
            Visibility: request.Visibility ?? DocumentVisibility.Private,
            Tags: ["generated", "template", request.TemplateId],
            Replace: false
        );

        return await documentService.UploadAsync(stream, fileName, pdfBytes.Length, "application/pdf", uploadRequest, ct);
    }

    /// <summary>
    /// Render a template to PDF and return as a stream (no storage).
    /// </summary>
    public async Task<(Stream Stream, string ContentType, string FileName)> GenerateAndStreamAsync(
        GenerateDocumentRequest request, CancellationToken ct = default)
    {
        var pdfBytes = await RenderToPdfAsync(request, ct);
        var fileName = ResolveFileName(request, ".pdf");
        var stream = new MemoryStream(pdfBytes);

        return (stream, "application/pdf", fileName);
    }

    private async Task<byte[]> RenderToPdfAsync(GenerateDocumentRequest request, CancellationToken ct)
    {
        var result = await templateClient.RenderTemplateAsync(
            request.TemplateId,
            request.Parameters,
            request.Language,
            ct);

        if (result == null)
            throw ServiceException.Of(DocumentServiceErrors.GenerationFailed,
                $"Template rendering returned no result for '{request.TemplateId}'");

        return await pdfRenderer.RenderHtmlToPdfAsync(result.RenderedContent, ct);
    }

    private static string ResolveFileName(GenerateDocumentRequest request, string extension)
    {
        var name = request.FileName ?? $"{request.TemplateId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        if (!name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            name += extension;
        return name;
    }
}
