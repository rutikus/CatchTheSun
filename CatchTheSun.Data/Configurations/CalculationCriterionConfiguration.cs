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
        builder.HasAlternateKey(x => new { x.CalculationId, x.CriterionId });
        builder.Property(x => x.Weight).HasPrecision(18, 8);
        builder.Property(x => x.NameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UnitSnapshot).HasMaxLength(50);
        builder.Property(x => x.DirectionSnapshot).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.TargetValueSnapshot).HasPrecision(20, 6);
        builder.Property(x => x.IsRequiredSnapshot).IsRequired();
        builder.HasIndex(x => x.CriterionId);
    }
}
