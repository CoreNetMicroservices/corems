using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.Common.Repository;
using CoreMs.Common.Security;
using CoreMs.TemplateMs.Core.Entities;
using CoreMs.TemplateMs.Core.Exceptions;
using CoreMs.TemplateMs.Core.Models;
using CoreMs.TemplateMs.Core.Repositories;

namespace CoreMs.TemplateMs.Core.Services;

[Service]
public class TemplateService(
    TemplateRepository repository,
    TemplateEngine engine,
    TemplateCache cache,
    ICurrentUserService currentUserService)
{
    private const int MaxPartialDepth = 10;

    public async Task<PagedResult<TemplateDto>> GetAllAsync(QueryParameters parameters, CancellationToken ct = default)
    {
        var result = await repository.GetPagedAsync(parameters, ct);
        return new PagedResult<TemplateDto>(
            result.Items.Select(MapToDto).ToList(),
            result.TotalElements,
            result.Page,
            result.PageSize);
    }

    public async Task<TemplateDto> GetByUuidAsync(Guid uuid, CancellationToken ct = default)
    {
        var entity = await repository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(TemplateErrors.TemplateNotFound, $"Template with UUID {uuid} not found");
        return MapToDto(entity);
    }

    public async Task<TemplateDto> CreateAsync(CreateTemplateRequest request, CancellationToken ct = default)
    {
        engine.ValidateSyntax(request.Content);

        var language = request.Language ?? "en";
        if (await repository.ExistsByTemplateIdAndLanguageAsync(request.TemplateId, language, ct))
            throw ServiceException.Of(TemplateErrors.TemplateAlreadyExists,
                $"Template '{request.TemplateId}' with language '{language}' already exists");

        var paramSchema = request.ParamSchema;
        if (paramSchema == null)
        {
            var extractedParams = engine.ExtractParameters(request.Content);
            paramSchema = extractedParams.ToDictionary(
                p => p,
                p => (object)new Dictionary<string, object> { ["type"] = "string", ["required"] = true });
        }

        var entity = new TemplateEntity
        {
            TemplateId = request.TemplateId,
            Language = language,
            Name = request.Name,
            Description = request.Description,
            Content = request.Content,
            Category = request.Category,
            ParamSchema = paramSchema,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = currentUserService.GetCurrentUserUuid()
        };

        repository.Add(entity);
        return MapToDto(entity);
    }

    public async Task<TemplateDto> UpdateAsync(Guid uuid, UpdateTemplateRequest request, CancellationToken ct = default)
    {
        var entity = await repository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(TemplateErrors.TemplateNotFound, $"Template with UUID {uuid} not found");

        if (request.Name != null) entity.Name = request.Name;
        if (request.Description != null) entity.Description = request.Description;
        if (request.Category != null) entity.Category = request.Category;
        if (request.TemplateId != null) entity.TemplateId = request.TemplateId;
        if (request.Language != null) entity.Language = request.Language;

        if (request.Content != null)
        {
            engine.ValidateSyntax(request.Content);
            entity.Content = request.Content;

            if (request.ParamSchema == null)
            {
                var extractedParams = engine.ExtractParameters(request.Content);
                entity.ParamSchema = extractedParams.ToDictionary(
                    p => p,
                    p => (object)new Dictionary<string, object> { ["type"] = "string", ["required"] = true });
            }
        }

        if (request.ParamSchema != null)
            entity.ParamSchema = request.ParamSchema;

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUserService.GetCurrentUserUuid();
        repository.Update(entity);

        cache.Invalidate(entity.TemplateId, entity.Language);

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid uuid, CancellationToken ct = default)
    {
        var entity = await repository.GetByUuidAsync(uuid, ct)
            ?? throw ServiceException.Of(TemplateErrors.TemplateNotFound, $"Template with UUID {uuid} not found");

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUserService.GetCurrentUserUuid();
        repository.Update(entity);

        cache.Invalidate(entity.TemplateId, entity.Language);
    }

    public async Task<RenderTemplateResponse> RenderAsync(RenderTemplateRequest request, CancellationToken ct = default)
    {
        var language = request.Language ?? "en";

        var entity = await repository.GetByTemplateIdAndLanguageAsync(request.TemplateId, language, ct)
            ?? throw ServiceException.Of(TemplateErrors.TemplateNotFound,
                $"Template '{request.TemplateId}' with language '{language}' not found");

        // Resolve all partial templates recursively
        var partials = await ResolvePartialsAsync(entity.Content, language, ct);

        // Validate parameters against this template + all partials
        ValidateRequiredParameters(entity, request.Parameters, partials);

        try
        {
            string rendered;
            if (partials.Count > 0)
            {
                var compiled = engine.CompileWithPartials(entity.Content, partials);
                rendered = engine.Render(compiled, request.Parameters);
            }
            else
            {
                var compiled = cache.Get(request.TemplateId, language);
                if (compiled == null)
                {
                    compiled = engine.Compile(entity.Content);
                    cache.Set(request.TemplateId, language, compiled);
                }
                rendered = engine.Render(compiled, request.Parameters);
            }

            return new RenderTemplateResponse(rendered);
        }
        catch (Exception ex) when (ex is not ServiceException)
        {
            throw ServiceException.Of(TemplateErrors.RenderingFailed,
                $"Failed to render template '{request.TemplateId}': {ex.Message}");
        }
    }

    public async Task<TemplateMetadataDto> GetMetadataAsync(string templateId, string? language, CancellationToken ct = default)
    {
        language ??= "en";
        var entity = await repository.GetByTemplateIdAndLanguageAsync(templateId, language, ct)
            ?? throw ServiceException.Of(TemplateErrors.TemplateNotFound,
                $"Template '{templateId}' with language '{language}' not found");

        // Include params from sub-templates in the metadata
        var partials = await ResolvePartialsAsync(entity.Content, language, ct);
        var allRequiredParams = GetAggregatedRequiredParameters(entity, partials);

        return new TemplateMetadataDto(
            entity.TemplateId,
            entity.Language,
            entity.Name,
            entity.Description,
            entity.Category,
            entity.ParamSchema,
            allRequiredParams);
    }

    /// <summary>
    /// Recursively resolve all partial templates referenced via {{> partialId}} syntax.
    /// Detects circular references and enforces a max depth.
    /// </summary>
    private async Task<Dictionary<string, string>> ResolvePartialsAsync(
        string content, string language, CancellationToken ct, HashSet<string>? visited = null, int depth = 0)
    {
        if (depth > MaxPartialDepth)
            throw ServiceException.Of(TemplateErrors.CircularPartialReference,
                "Exceeded maximum partial template nesting depth");

        visited ??= new HashSet<string>(StringComparer.Ordinal);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        var partialIds = engine.ExtractPartialReferences(content);
        if (partialIds.Count == 0) return result;

        foreach (var partialId in partialIds)
        {
            if (result.ContainsKey(partialId)) continue; // Already resolved in this tree

            if (!visited.Add(partialId))
                throw ServiceException.Of(TemplateErrors.CircularPartialReference,
                    $"Circular reference detected: partial '{partialId}' references itself");

            var partialEntity = await repository.GetByTemplateIdAndLanguageAsync(partialId, language, ct)
                ?? throw ServiceException.Of(TemplateErrors.PartialNotFound,
                    $"Partial template '{partialId}' with language '{language}' not found");

            result[partialId] = partialEntity.Content;

            // Recursively resolve nested partials
            var nested = await ResolvePartialsAsync(partialEntity.Content, language, ct, visited, depth + 1);
            foreach (var (key, value) in nested)
            {
                result.TryAdd(key, value);
            }
        }

        return result;
    }

    private void ValidateRequiredParameters(
        TemplateEntity entity, Dictionary<string, object> providedParams, Dictionary<string, string> partials)
    {
        var allRequired = GetAggregatedRequiredParameters(entity, partials);
        var missing = allRequired.Where(p => !providedParams.ContainsKey(p)).ToList();

        if (missing.Count > 0)
            throw ServiceException.Of(TemplateErrors.MissingRequiredParameters,
                $"Missing parameters: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// Get required parameters from the main template plus all its partial sub-templates.
    /// </summary>
    private IReadOnlyList<string> GetAggregatedRequiredParameters(
        TemplateEntity entity, Dictionary<string, string> partials)
    {
        var allRequired = new HashSet<string>(GetRequiredParameterNames(entity), StringComparer.Ordinal);

        // Extract parameters used in partials content (these are rendered with the same context)
        foreach (var partialContent in partials.Values)
        {
            var partialParams = engine.ExtractParameters(partialContent);
            foreach (var p in partialParams)
                allRequired.Add(p);
        }

        return allRequired.Order().ToList();
    }

    private static IReadOnlyList<string> GetRequiredParameterNames(TemplateEntity entity)
    {
        if (entity.ParamSchema == null) return [];

        return entity.ParamSchema
            .Where(kvp =>
            {
                if (kvp.Value is Dictionary<string, object> schema &&
                    schema.TryGetValue("required", out var req))
                    return req is true or "true" or "True";
                return true;
            })
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private static TemplateDto MapToDto(TemplateEntity entity) => new(
        entity.Uuid,
        entity.TemplateId,
        entity.Language,
        entity.Name,
        entity.Description,
        entity.Content,
        entity.Category,
        entity.ParamSchema,
        entity.CreatedAt,
        entity.UpdatedAt,
        entity.CreatedBy,
        entity.UpdatedBy);
}
