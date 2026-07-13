using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.Common.Repository;
using CoreMs.Common.Security;
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
public class DocumentService(
    DocumentRepository documentRepository,
    DocumentAccessTokenRepository documentAccessTokenRepository,
    S3StorageService storageService,
    ICurrentUserService currentUserService,
    IOptions<DocumentOptions> documentOptions,
    IOptions<StorageOptions> storageOptions,
    ILogger<DocumentService> logger)
{
    private readonly DocumentOptions _documentOptions = documentOptions.Value;
    private readonly StorageOptions _storageOptions = storageOptions.Value;

    public async Task<DocumentDto> UploadAsync(Stream fileStream, string originalFilename, long size, string contentType, UploadDocumentRequest request, CancellationToken ct = default)
    {
        var currentUser = currentUserService.GetCurrentUserUuid();
        var extension = ExtractExtension(originalFilename);

        ValidateFileSize(size);
        ValidateExtension(extension);

        var checksum = await ComputeChecksumAsync(fileStream, ct);
        fileStream.Position = 0;

        if (request.Replace)
        {
            var existing = await documentRepository.GetByUserIdAndFilenameAsync(currentUser, originalFilename, ct);
            if (existing is not null)
            {
                try
                {
                    await storageService.DeleteObjectAsync(existing.ObjectKey, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete existing object {ObjectKey} during replacement", existing.ObjectKey);
                    throw ServiceException.Of(DocumentServiceErrors.StorageError);
                }
                documentRepository.Remove(existing);
            }
        }

        var objectKey = GenerateObjectKey(currentUser, originalFilename);

        try
        {
            await storageService.UploadObjectAsync(fileStream, objectKey, contentType, size, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload file to storage: {ObjectKey}", objectKey);
            throw ServiceException.Of(DocumentServiceErrors.StorageError);
        }

        var entity = new DocumentEntity
        {
            UserId = currentUser,
            Name = request.Name ?? Path.GetFileNameWithoutExtension(originalFilename),
            OriginalFilename = originalFilename,
            Size = size,
            Extension = extension,
            ContentType = contentType,
            Bucket = _storageOptions.Bucket,
            ObjectKey = objectKey,
            Visibility = request.Visibility ?? DocumentVisibility.Private,
            UploadedById = currentUser,
            UploadedByType = UploadedByType.User,
            Checksum = checksum,
            Description = request.Description,
            Tags = request.Tags ?? [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        documentRepository.Add(entity);

        return MapToDto(entity);
    }

    public async Task<DocumentDto> UploadBase64Async(UploadBase64Request request, CancellationToken ct = default)
    {
        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(request.ContentBase64);
        }
        catch (FormatException)
        {
            throw ServiceException.Of(DocumentServiceErrors.InvalidBase64);
        }

        var extension = ExtractExtension(request.FileName);
        ValidateFileSize(fileBytes.Length);
        ValidateExtension(extension);

        var contentType = DetermineContentType(extension);
        using var stream = new MemoryStream(fileBytes);

        var uploadRequest = new UploadDocumentRequest(
            request.Name,
            request.Description,
            request.Visibility,
            request.Tags,
            request.Replace
        );

        return await UploadAsync(stream, request.FileName, fileBytes.Length, contentType, uploadRequest, ct);
    }

    public async Task<DocumentDto> GetDocumentAsync(Guid uuid, CancellationToken ct = default)
    {
        var entity = await documentRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(DocumentServiceErrors.DocumentNotFound);

        EnsureAccessAllowed(entity);

        return MapToDto(entity);
    }

    public async Task<(Stream Stream, string ContentType, string Filename)> StreamDocumentAsync(Guid uuid, CancellationToken ct = default)
    {
        var entity = await documentRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(DocumentServiceErrors.DocumentNotFound);

        EnsureAccessAllowed(entity);

        try
        {
            var stream = await storageService.GetObjectStreamAsync(entity.ObjectKey, ct);
            return (stream, entity.ContentType, entity.OriginalFilename);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stream document {ObjectKey}", entity.ObjectKey);
            throw ServiceException.Of(DocumentServiceErrors.StorageError);
        }
    }

    public async Task<PagedResult<DocumentDto>> ListDocumentsAsync(QueryParameters parameters, CancellationToken ct = default)
    {
        var result = await documentRepository.GetPagedAsync(parameters, ct);

        return new PagedResult<DocumentDto>(
            result.Items.Select(MapToDto).ToList(),
            result.TotalElements,
            result.Page,
            result.PageSize
        );
    }

    private void EnsureAccessAllowed(DocumentEntity entity)
    {
        if (entity.Visibility == DocumentVisibility.Public)
            return;

        var currentUser = currentUserService.GetCurrentUserUuid();
        var roles = currentUserService.GetCurrentUserRoles();

        if (entity.UserId == currentUser)
            return;

        if (roles.Contains(CoreMsRoles.DocumentMsAdmin) || roles.Contains(CoreMsRoles.SuperAdmin))
            return;

        throw ServiceException.Of(DocumentServiceErrors.DocumentAccessDenied);
    }

    public static string ExtractExtension(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return string.Empty;

        var lastDotIndex = filename.LastIndexOf('.');
        if (lastDotIndex < 0 || lastDotIndex == filename.Length - 1)
            return string.Empty;

        return filename[(lastDotIndex + 1)..].ToLowerInvariant();
    }

    private void ValidateFileSize(long size)
    {
        if (size > _documentOptions.MaxUploadSizeBytes)
            throw ServiceException.Of(DocumentServiceErrors.FileTooLarge);
    }

    private void ValidateExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return;

        if (!_documentOptions.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw ServiceException.Of(DocumentServiceErrors.FileExtensionNotAllowed);
    }

    private static async Task<string> ComputeChecksumAsync(Stream stream, CancellationToken ct)
    {
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexStringLower(hash);
    }

    private static string GenerateObjectKey(Guid userId, string filename)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var extension = ExtractExtension(filename);
        var safeName = Path.GetFileNameWithoutExtension(filename);

        return $"{userId}/{timestamp}_{uniqueId}_{safeName}.{extension}";
    }

    private static string DetermineContentType(string extension) => extension.ToLowerInvariant() switch
    {
        "pdf" => "application/pdf",
        "png" => "image/png",
        "jpg" or "jpeg" => "image/jpeg",
        "gif" => "image/gif",
        "doc" => "application/msword",
        "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "xls" => "application/vnd.ms-excel",
        "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "txt" => "text/plain",
        "csv" => "text/csv",
        "zip" => "application/zip",
        _ => "application/octet-stream"
    };

    public async Task<DocumentDto> UpdateDocumentAsync(Guid uuid, UpdateDocumentRequest request, CancellationToken ct = default)
    {
        var entity = await documentRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(DocumentServiceErrors.DocumentNotFound);

        if (request.Name is not null)
            entity.Name = request.Name;

        if (request.Description is not null)
            entity.Description = request.Description;

        if (request.Visibility is not null)
            entity.Visibility = request.Visibility.Value;

        if (request.Tags is not null)
            entity.Tags = request.Tags;

        entity.UpdatedAt = DateTime.UtcNow;

        documentRepository.Update(entity);

        return MapToDto(entity);
    }

    public async Task DeleteDocumentAsync(Guid uuid, bool permanent, CancellationToken ct = default)
    {
        var entity = permanent
            ? await documentRepository.GetByUuidIncludeDeletedAsync(uuid, ct)
            : await documentRepository.GetByUuidAsync(uuid, ct);

        if (entity is null)
            throw ServiceException.Of(DocumentServiceErrors.DocumentNotFound);

        if (!permanent && entity.IsDeleted)
            throw ServiceException.Of(DocumentServiceErrors.DocumentAlreadyDeleted);

        if (permanent)
        {
            try
            {
                await storageService.DeleteObjectAsync(entity.ObjectKey, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete object from storage: {ObjectKey}", entity.ObjectKey);
                throw ServiceException.Of(DocumentServiceErrors.StorageError);
            }

            documentRepository.Remove(entity);
        }
        else
        {
            var currentUser = currentUserService.GetCurrentUserUuid();
            entity.IsDeleted = true;
            entity.DeletedBy = currentUser;
            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            documentRepository.Update(entity);
        }
    }

    internal static DocumentDto MapToDto(DocumentEntity entity) => new(
        entity.Uuid,
        entity.UserId,
        entity.Name,
        entity.OriginalFilename,
        entity.Size,
        entity.Extension,
        entity.ContentType,
        entity.Visibility,
        entity.UploadedById,
        entity.UploadedByType,
        entity.Checksum,
        entity.Description,
        entity.Tags,
        entity.Version,
        entity.CreatedAt,
        entity.UpdatedAt
    );

    public async Task<DocumentLinkDto> GenerateAccessLinkAsync(Guid uuid, GenerateLinkRequest request, CancellationToken ct = default)
    {
        var entity = await documentRepository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(DocumentServiceErrors.DocumentNotFound);

        if (entity.Visibility != DocumentVisibility.ByLink)
            throw ServiceException.Of(DocumentServiceErrors.LinkGenerationNotAllowed);

        var currentUser = currentUserService.GetCurrentUserUuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(request.ExpiresInMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_documentOptions.LinkSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", "document-access"),
                new Claim("doc", uuid.ToString())
            ]),
            IssuedAt = DateTime.UtcNow,
            Expires = expiresAt,
            SigningCredentials = credentials
        };

        var tokenHandler = new JsonWebTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        var tokenHash = ComputeTokenHash(token);
        var accessToken = new DocumentAccessTokenEntity
        {
            DocumentUuid = uuid,
            TokenHash = tokenHash,
            CreatedBy = currentUser,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        documentAccessTokenRepository.Add(accessToken);

        var url = $"{_documentOptions.BaseUrl}/api/public/documents/link/{token}";

        return new DocumentLinkDto(token, url, expiresAt);
    }

    internal static string ComputeTokenHash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
