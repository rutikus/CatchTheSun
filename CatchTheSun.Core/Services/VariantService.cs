using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;

namespace CatchTheSun.Core.Services;

public sealed class VariantService(IVariantRepository repository)
{
    public async Task<IReadOnlyList<VariantDto>?> GetBySiteIdAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        if (!await repository.SiteExistsAsync(siteId, cancellationToken))
            return null;

        var variants = await repository.GetBySiteIdAsync(siteId, cancellationToken);
        return variants.Select(Map).ToArray();
    }

    public async Task<VariantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var variant = await repository.GetByIdAsync(id, cancellationToken);
        return variant is null ? null : Map(variant);
    }

    public async Task<VariantDto?> CreateAsync(Guid siteId, CreateVariantDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        if (!await repository.SiteExistsAsync(siteId, cancellationToken))
            return null;

        var variant = new Variant
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            Name = dto.Name!.Trim(),
            ExternalId = NormalizeExternalId(dto.ExternalId),
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(variant, cancellationToken);
        return Map(variant);
    }

    public async Task<VariantDto?> UpdateAsync(Guid id, UpdateVariantDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        var variant = await repository.GetByIdAsync(id, cancellationToken);
        if (variant is null)
            return null;

        variant.Name = dto.Name!.Trim();
        variant.ExternalId = NormalizeExternalId(dto.ExternalId);
        await repository.UpdateAsync(variant, cancellationToken);
        return Map(variant);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var variant = await repository.GetByIdAsync(id, cancellationToken);
        if (variant is null)
            return false;

        await repository.DeleteAsync(variant, cancellationToken);
        return true;
    }

    private static void Validate(object dto)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true))
            throw new ValidationException(results[0].ErrorMessage);
    }

    private static string? NormalizeExternalId(string? externalId) =>
        string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim();

    private static VariantDto Map(Variant variant) => new()
    {
        Id = variant.Id,
        SiteId = variant.SiteId,
        Name = variant.Name,
        ExternalId = variant.ExternalId,
        CreatedAt = variant.CreatedAt
    };
}
