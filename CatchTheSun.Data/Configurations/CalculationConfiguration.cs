using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public class CalculationConfiguration : IEntityTypeConfiguration<Calculation>
{
    public void Configure(EntityTypeBuilder<Calculation> builder)
    {
        builder.ToTable("Calculations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.SiteId);
        builder.HasMany(x => x.Criteria).WithOne(x => x.Calculation).HasForeignKey(x => x.CalculationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.CriterionComparisons).WithOne(x => x.Calculation).HasForeignKey(x => x.CalculationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Results).WithOne(x => x.Calculation).HasForeignKey(x => x.CalculationId).OnDelete(DeleteBehavior.Cascade);
    }
}
