using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public class CriterionConfiguration : IEntityTypeConfiguration<Criterion>
{
    public void Configure(EntityTypeBuilder<Criterion> builder)
    {
        builder.ToTable("Criteria");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Unit).HasMaxLength(50);
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.TargetValue).HasPrecision(20, 6);
        builder.Property(x => x.IsRequired).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasMany(x => x.Values).WithOne(x => x.Criterion).HasForeignKey(x => x.CriterionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.CalculationCriteria).WithOne(x => x.Criterion).HasForeignKey(x => x.CriterionId).OnDelete(DeleteBehavior.Restrict);
    }
}
