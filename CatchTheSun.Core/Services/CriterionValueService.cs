using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;

namespace CatchTheSun.Core.Services;

public sealed class CriterionValueService(ICriterionValueRepository repository)
{
    public async Task<IReadOnlyList<CriterionValueDto>?> GetByVariantIdAsync(Guid variantId, CancellationToken ct = default)
    {
        if (!await repository.VariantExistsAsync(variantId, ct)) return null;
        return (await repository.GetByVariantIdAsync(variantId, ct)).Select(Map).ToArray();
    }
    public async Task<CriterionValueDto?> GetByIdAsync(Guid id, CancellationToken ct = default) => (await repository.GetByIdAsync(id, ct)) is { } x ? Map(x) : null;
    public async Task<CriterionValueDto?> CreateAsync(Guid variantId, CreateCriterionValueDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        if (!await repository.VariantExistsAsync(variantId, ct) || !await repository.CriterionExistsAsync(dto.CriterionId, ct)) return null;
        if (!await repository.CriterionIsActiveAsync(dto.CriterionId, ct)) throw new InvalidOperationException("Inactive criteria cannot receive new values.");
        if (await repository.ExistsAsync(variantId, dto.CriterionId, ct)) throw new InvalidOperationException("A value already exists for this variant and criterion.");
        var entity = new CriterionValue { Id = Guid.NewGuid(), VariantId = variantId, CriterionId = dto.CriterionId, Value = dto.Value };
        await repository.AddAsync(entity, ct);
        return Map(entity);
    }
    public async Task<CriterionValueDto?> UpdateAsync(Guid id, UpdateCriterionValueDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return null;
        entity.Value = dto.Value;
        await repository.UpdateAsync(entity, ct);
        return Map(entity);
    }
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return false;
        await repository.DeleteAsync(entity, ct);
        return true;
    }
    private static void Validate(object dto) { var errors = new List<ValidationResult>(); if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true)) throw new ValidationException(errors[0].ErrorMessage); }
    private static CriterionValueDto Map(CriterionValue x) => new() { Id = x.Id, VariantId = x.VariantId, CriterionId = x.CriterionId, Value = x.Value };
}
