using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatchTheSun.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<Criterion> Criteria => Set<Criterion>();
    public DbSet<CriterionValue> CriterionValues => Set<CriterionValue>();
    public DbSet<Calculation> Calculations => Set<Calculation>();
    public DbSet<CalculationCriterion> CalculationCriteria => Set<CalculationCriterion>();
    public DbSet<CriterionComparison> CriterionComparisons => Set<CriterionComparison>();
    public DbSet<CalculationResult> CalculationResults => Set<CalculationResult>();
    public DbSet<DirectSunImport> DirectSunImports => Set<DirectSunImport>();
    public DbSet<DirectSunMeasurementRecord> DirectSunMeasurements => Set<DirectSunMeasurementRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
