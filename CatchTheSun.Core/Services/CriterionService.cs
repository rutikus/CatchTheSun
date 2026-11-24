using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Exceptions;
using CatchTheSun.Core.Interfaces;

namespace CatchTheSun.Core.Services;

public sealed class CriterionService(ICriterionRepository repository)
{
    public async Task<IReadOnlyList<CriterionDto>> GetAllAsync(CancellationToken token = default) =>
        (await repository.GetAllAsync(token)).Select(Map).ToArray();

    public async Task<CriterionDto?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        var entity = await repository.GetByIdAsync(id, token);
        return entity is null ? null : Map(entity);
    }

    public async Task<CriterionDto> CreateAsync(CreateCriterionDto dto, CancellationToken token = default)
    {
        Validate(dto);
        var name = dto.Name!.Trim();
        if (await repository.ExistsByNameAsync(name, cancellationToken: token))
            throw new DuplicateCriterionNameException(name);
        var entity = new Criterion { Id = Guid.NewGuid(), Name = name, Description = Clean(dto.Description), Unit = Clean(dto.Unit), Direction = dto.Direction, TargetValue = dto.TargetValue, IsRequired = dto.IsRequired, IsActive = dto.IsActive, CreatedAt = DateTime.UtcNow };
        await repository.AddAsync(entity, token);
        return Map(entity);
    }

    public async Task<CriterionDto?> UpdateAsync(Guid id, UpdateCriterionDto dto, CancellationToken token = default)
    {
        Validate(dto);
        var entity = await repository.GetByIdAsync(id, token);
        if (entity is null) return null;
        var name = dto.Name!.Trim();
        if (await repository.ExistsByNameAsync(name, id, token)) throw new DuplicateCriterionNameException(name);
        entity.Name = name; entity.Description = Clean(dto.Description); entity.Unit = Clean(dto.Unit);
        entity.Direction = dto.Direction; entity.TargetValue = dto.TargetValue;
        entity.IsRequired = dto.IsRequired; entity.IsActive = dto.IsActive;
        await repository.UpdateAsync(entity, token);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken token = default)
    {
        var entity = await repository.GetByIdAsync(id, token);
        if (entity is null) return false;
        if (await repository.HasDependenciesAsync(id, token)) throw new CriterionHasDependenciesException(id);
        await repository.DeleteAsync(entity, token);
        return true;
    }

    private static void Validate(object dto)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true)) throw new ValidationException(errors[0].ErrorMessage);
        var (direction, target) = dto switch
        {
            CreateCriterionDto c => (c.Direction, c.TargetValue),
            UpdateCriterionDto u => (u.Direction, u.TargetValue),
            _ => throw new ArgumentException("Unsupported criterion DTO.", nameof(dto))
        };
        if (!Enum.IsDefined(direction)) throw new ValidationException("Direction is invalid.");
        if (direction == CriterionDirection.Target && target is null) throw new ValidationException("TargetValue is required for Target direction.");
        if (direction != CriterionDirection.Target && target is not null) throw new ValidationException("TargetValue is only valid for Target direction.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static CriterionDto Map(Criterion x) => new() { Id = x.Id, Name = x.Name, Description = x.Description, Unit = x.Unit, Direction = x.Direction, TargetValue = x.TargetValue, IsRequired = x.IsRequired, IsActive = x.IsActive, CreatedAt = x.CreatedAt };
}
