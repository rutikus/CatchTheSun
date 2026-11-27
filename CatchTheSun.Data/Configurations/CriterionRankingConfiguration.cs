using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public sealed class CriterionRankingConfiguration : IEntityTypeConfiguration<CriterionRanking>
{
    public void Configure(EntityTypeBuilder<CriterionRanking> b)
    {
        b.ToTable("CriterionRankings", t => t.HasCheckConstraint("CK_CriterionRankings_Rank", "\"Rank\" >= 1"));
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ExpertResponseId, x.CriterionId }).IsUnique();
        b.HasOne(x => x.ExpertResponse).WithMany(x => x.Rankings)
            .HasForeignKey(x => new { x.ExpertResponseId, x.QuestionnaireId })
            .HasPrincipalKey(x => new { x.Id, x.QuestionnaireId }).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<QuestionnaireCriterion>().WithMany().HasForeignKey(x => new { x.QuestionnaireId, x.CriterionId })
            .HasPrincipalKey(x => new { x.QuestionnaireId, x.CriterionId }).OnDelete(DeleteBehavior.Cascade);
    }
}
