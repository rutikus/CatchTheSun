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
    public DbSet<UrbanSketchImport> UrbanSketchImports => Set<UrbanSketchImport>();
    public DbSet<UrbanSketchMeasurementRecord> UrbanSketchMeasurements => Set<UrbanSketchMeasurementRecord>();
    public DbSet<InsolationComparison> InsolationComparisons => Set<InsolationComparison>();
    public DbSet<InsolationComparisonPoint> InsolationComparisonPoints => Set<InsolationComparisonPoint>();
    public DbSet<Expert> Experts => Set<Expert>();
    public DbSet<Questionnaire> Questionnaires => Set<Questionnaire>();
    public DbSet<QuestionnaireCriterion> QuestionnaireCriteria => Set<QuestionnaireCriterion>();
    public DbSet<ExpertResponse> ExpertResponses => Set<ExpertResponse>();
    public DbSet<PairwiseComparison> PairwiseComparisons => Set<PairwiseComparison>();
    public DbSet<CriterionRanking> CriterionRankings => Set<CriterionRanking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
