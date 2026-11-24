using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;

namespace CatchTheSun.Core.Services;

public sealed class SiteService(ISiteRepository repository)
{
    public async Task<IReadOnlyList<SiteDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sites = await repository.GetAllAsync(cancellationToken);
        return sites.Select(Map).ToArray();
    }

    public async Task<SiteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var site = await repository.GetByIdAsync(id, cancellationToken);
        return site is null ? null : Map(site);
    }

    public async Task<SiteDto> CreateAsync(CreateSiteDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);

        var site = new Site
        {
            Id = Guid.NewGuid(),
            Name = dto.Name!.Trim(),
            Location = dto.Location!.Trim(),
            Area = dto.Area,
            CreatedAt = DateTime.UtcNow
        };

        repository.Add(site);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(site);
    }

    public async Task<SiteDto?> UpdateAsync(Guid id, UpdateSiteDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);

        var site = await repository.GetByIdAsync(id, cancellationToken);
        if (site is null)
            return null;

        site.Name = dto.Name!.Trim();
        site.Location = dto.Location!.Trim();
        site.Area = dto.Area;

        await repository.SaveChangesAsync(cancellationToken);
        return Map(site);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var site = await repository.GetByIdAsync(id, cancellationToken);
        if (site is null)
            return false;

        repository.Delete(site);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Validate(object dto)
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), validationResults, validateAllProperties: true))
            throw new ValidationException(validationResults[0].ErrorMessage);

        var area = dto switch
        {
            CreateSiteDto create => create.Area,
            UpdateSiteDto update => update.Area,
            _ => throw new ArgumentException("Unsupported site DTO type.", nameof(dto))
        };

        if (area <= 0)
            throw new ValidationException("Area must be greater than zero.");
    }

    private static SiteDto Map(Site site) => new()
    {
        Id = site.Id,
        Name = site.Name,
        Location = site.Location,
        Area = site.Area,
        CreatedAt = site.CreatedAt
    };
}
