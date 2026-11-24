using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public class CriterionComparisonConfiguration : IEntityTypeConfiguration<CriterionComparison>
{
    public void Configure(EntityTypeBuilder<CriterionComparison> builder)
    {
        builder.ToTable("CriterionComparisons", table => table.HasCheckConstraint(
            "CK_CriterionComparisons_ValidPairAndSaatyValue",
            "\"CriterionAId\" < \"CriterionBId\" AND " +
            "(\"Value\" IN (1, 2, 3, 4, 5, 6, 7, 8, 9) OR " +
            "\"Value\" IN (0.5, 0.3333333333333333333333333333, 0.25, 0.2, 0.1666666666666666666666666667, 0.1428571428571428571428571429, 0.125, 0.1111111111111111111111111111))"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).HasPrecision(28, 27).IsRequired();
        builder.HasIndex(x => new { x.CalculationId, x.CriterionAId, x.CriterionBId }).IsUnique();
        builder.HasIndex(x => new { x.CalculationId, x.CriterionBId });
        builder.HasOne(x => x.CriterionA).WithMany().HasForeignKey(x => x.CriterionAId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CriterionB).WithMany().HasForeignKey(x => x.CriterionBId).OnDelete(DeleteBehavior.Restrict);
    }
}
