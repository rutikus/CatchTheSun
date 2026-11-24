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
        builder.Property(x => x.Score).HasPrecision(20, 8);
        builder.HasIndex(x => new { x.CalculationId, x.VariantId }).IsUnique();
        builder.HasIndex(x => x.VariantId);
    }
}
