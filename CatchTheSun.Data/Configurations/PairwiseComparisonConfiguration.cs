using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public sealed class PairwiseComparisonConfiguration : IEntityTypeConfiguration<PairwiseComparison>
{
    public void Configure(EntityTypeBuilder<PairwiseComparison> b)
    {
        b.ToTable("PairwiseComparisons", t => t.HasCheckConstraint("CK_PairwiseComparisons_SaatyAndOrder",
            "\"FirstCriterionId\" < \"SecondCriterionId\" AND (\"Value\" IN (1,2,3,4,5,6,7,8,9) OR \"Value\" IN (0.5,0.3333333333333333333333333333,0.25,0.2,0.1666666666666666666666666667,0.1428571428571428571428571429,0.125,0.1111111111111111111111111111))"));
        b.HasKey(x => x.Id); b.Property(x => x.Value).HasPrecision(29, 28).IsRequired();
        b.HasIndex(x => new { x.ExpertResponseId, x.FirstCriterionId, x.SecondCriterionId }).IsUnique();
        b.HasOne(x => x.ExpertResponse).WithMany(x => x.PairwiseComparisons)
            .HasForeignKey(x => new { x.ExpertResponseId, x.QuestionnaireId })
            .HasPrincipalKey(x => new { x.Id, x.QuestionnaireId }).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<QuestionnaireCriterion>().WithMany().HasForeignKey(x => new { x.QuestionnaireId, x.FirstCriterionId })
            .HasPrincipalKey(x => new { x.QuestionnaireId, x.CriterionId }).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<QuestionnaireCriterion>().WithMany().HasForeignKey(x => new { x.QuestionnaireId, x.SecondCriterionId })
            .HasPrincipalKey(x => new { x.QuestionnaireId, x.CriterionId }).OnDelete(DeleteBehavior.Cascade);
    }
}
