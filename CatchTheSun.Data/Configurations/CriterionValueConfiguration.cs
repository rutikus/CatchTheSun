using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public class CriterionValueConfiguration : IEntityTypeConfiguration<CriterionValue>
{
    public void Configure(EntityTypeBuilder<CriterionValue> builder)
    {
        builder.ToTable("CriterionValues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).HasPrecision(20, 6).IsRequired();
        builder.HasIndex(x => new { x.VariantId, x.CriterionId }).IsUnique();
        builder.HasIndex(x => x.CriterionId);
    }
}
