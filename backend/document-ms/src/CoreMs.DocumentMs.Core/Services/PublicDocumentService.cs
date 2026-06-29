using System.Text;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.DocumentMs.Core.Configuration;
using CoreMs.DocumentMs.Core.Entities;
using CoreMs.DocumentMs.Core.Enums;
using CoreMs.DocumentMs.Core.Exceptions;
using CoreMs.DocumentMs.Core.Models;
using CoreMs.DocumentMs.Core.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CoreMs.DocumentMs.Core.Services;

[Service]
public class PublicDocumentService(
    DocumentRepository documentRepository,
    DocumentAccessTokenRepository documentAccessTokenRepository,
    S3StorageService storageService,
    IOptions<DocumentOptions> documentOptions,
    ILogger<PublicDocumentService> logger)
{
    private readonly DocumentOptions _documentOptions = documentOptions.Value;

    public async Task<DocumentDto> GetPublicDocumentAsync(Guid uuid, CancellationToken ct = default)
    {
        var entity = await documentRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(DocumentServiceErrors.DocumentNotFound);

        if (entity.Visibility != DocumentVisibility.Public)
            throw ServiceException.Of(DocumentServiceErrors.DocumentAccessDenied);

        return DocumentService.MapToDto(entity);
    }

    public async Task<(Stream Stream, string ContentType, string Filename)> StreamPublicDocumentAsync(Guid uuid, CancellationToken ct = default)
    {
        var entity = await documentRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(DocumentServiceErrors.DocumentNotFound);

        if (entity.Visibility != DocumentVisibility.Public)
            throw ServiceException.Of(DocumentServiceErrors.DocumentAccessDenied);

        try
        {
            var stream = await storageService.GetObjectStreamAsync(entity.ObjectKey, ct);
            return (stream, entity.ContentType, entity.OriginalFilename);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stream public document {ObjectKey}", entity.ObjectKey);
            throw ServiceException.Of(DocumentServiceErrors.StorageError);
        }
    }

    public async Task<DocumentDto> GetDocumentByTokenAsync(string token, CancellationToken ct = default)
    {
        var (entity, tokenEntity) = await ValidateTokenAndGetDocumentAsync(token, ct);

        tokenEntity.AccessCount++;
        tokenEntity.LastAccessedAt = DateTime.UtcNow;
        documentAccessTokenRepository.Update(tokenEntity);

        return DocumentService.MapToDto(entity);
    }

    public async Task<(Stream Stream, string ContentType, string Filename)> StreamDocumentByTokenAsync(string token, CancellationToken ct = default)
    {
        var (entity, tokenEntity) = await ValidateTokenAndGetDocumentAsync(token, ct);

        tokenEntity.AccessCount++;
        tokenEntity.LastAccessedAt = DateTime.UtcNow;
        documentAccessTokenRepository.Update(tokenEntity);

        try
        {
            var stream = await storageService.GetObjectStreamAsync(entity.ObjectKey, ct);
            return (stream, entity.ContentType, entity.OriginalFilename);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stream token-accessed document {ObjectKey}", entity.ObjectKey);
            throw ServiceException.Of(DocumentServiceErrors.StorageError);
        }
    }

    private async Task<(DocumentEntity Document, DocumentAccessTokenEntity Token)> ValidateTokenAndGetDocumentAsync(string token, CancellationToken ct)
    {
        Guid documentUuid;
        try
        {
            var handler = new JsonWebTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_documentOptions.LinkSigningKey));

            var validationResult = await handler.ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            });

            if (!validationResult.IsValid)
            {
                if (validationResult.Exception is SecurityTokenExpiredException)
                    throw ServiceException.Of(DocumentServiceErrors.TokenExpired);

                throw ServiceException.Of(DocumentServiceErrors.TokenInvalid);
            }

            var hasClaim = validationResult.Claims.TryGetValue("doc", out var docClaimValue);
            var docClaim = docClaimValue?.ToString();
            if (!hasClaim || docClaim is null || !Guid.TryParse(docClaim, out documentUuid))
                throw ServiceException.Of(DocumentServiceErrors.TokenInvalid);
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception)
        {
            throw ServiceException.Of(DocumentServiceErrors.TokenInvalid);
        }

        var tokenHash = DocumentService.ComputeTokenHash(token);
        var tokenEntity = await documentAccessTokenRepository.GetByTokenHashAndDocumentAsync(tokenHash, documentUuid, ct)
            ?? throw ServiceException.Of(DocumentServiceErrors.TokenInvalid);

        if (tokenEntity.IsRevoked)
            throw ServiceException.Of(DocumentServiceErrors.TokenRevoked);

        if (tokenEntity.ExpiresAt <= DateTime.UtcNow)
            throw ServiceException.Of(DocumentServiceErrors.TokenExpired);

        var entity = await documentRepository.GetByUuidAsync(documentUuid, ct)
            ?? throw ServiceException.Of(DocumentServiceErrors.DocumentNotFound);

        if (entity.Visibility != DocumentVisibility.ByLink)
            throw ServiceException.Of(DocumentServiceErrors.DocumentNotFound);

        return (entity, tokenEntity);
    }
}
