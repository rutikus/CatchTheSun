using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public class CalculationCriterionConfiguration : IEntityTypeConfiguration<CalculationCriterion>
{
    public void Configure(EntityTypeBuilder<CalculationCriterion> builder)
    {
        builder.ToTable("CalculationCriteria");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Weight).HasPrecision(18, 8).IsRequired();
        builder.Property(x => x.DirectionSnapshot).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.TargetValueSnapshot).HasPrecision(20, 6);
        builder.HasIndex(x => new { x.CalculationId, x.CriterionId }).IsUnique();
        builder.HasIndex(x => x.CriterionId);
    }
}
