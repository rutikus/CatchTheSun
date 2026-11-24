using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public class CalculationResultCriterionSnapshotConfiguration : IEntityTypeConfiguration<CalculationResultCriterionSnapshot>
{
    public void Configure(EntityTypeBuilder<CalculationResultCriterionSnapshot> builder)
    {
        builder.ToTable("CalculationResultCriterionSnapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ValueSnapshot).HasPrecision(20, 6).IsRequired();
        builder.Property(x => x.DirectionSnapshot).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.TargetValueSnapshot).HasPrecision(20, 6);
        builder.Property(x => x.WeightSnapshot).HasPrecision(18, 8).IsRequired();
        builder.HasAlternateKey(x => new { x.CalculationResultId, x.CriterionId });
        builder.HasIndex(x => new { x.CalculationId, x.CriterionId });
        builder.HasOne<CalculationCriterion>().WithMany()
            .HasForeignKey(x => new { x.CalculationId, x.CriterionId })
            .HasPrincipalKey(x => new { x.CalculationId, x.CriterionId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
