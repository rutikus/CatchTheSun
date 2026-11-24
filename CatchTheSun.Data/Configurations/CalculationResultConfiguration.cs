using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public class CalculationResultConfiguration : IEntityTypeConfiguration<CalculationResult>
{
    public void Configure(EntityTypeBuilder<CalculationResult> builder)
    {
        builder.ToTable("CalculationResults");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.Id, x.CalculationId });
        builder.Property(x => x.VariantNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ExternalIdSnapshot).HasMaxLength(200);
        builder.Property(x => x.Score).HasPrecision(20, 8);
        builder.Property(x => x.ParetoSecondaryScore).HasPrecision(20, 8);
        builder.HasIndex(x => new { x.CalculationId, x.VariantId }).IsUnique();
        builder.HasIndex(x => x.VariantId);
        builder.HasMany(x => x.InputSnapshots).WithOne(x => x.CalculationResult)
            .HasForeignKey(x => new { x.CalculationResultId, x.CalculationId })
            .HasPrincipalKey(x => new { x.Id, x.CalculationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
