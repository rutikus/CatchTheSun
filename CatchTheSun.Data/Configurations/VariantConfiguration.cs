using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> builder)
    {
        builder.ToTable("Variants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ExternalId).HasMaxLength(200);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.SiteId);
        builder.HasMany(x => x.CriterionValues).WithOne(x => x.Variant).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.CalculationResults).WithOne(x => x.Variant).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
