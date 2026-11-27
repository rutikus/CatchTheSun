using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public sealed class QuestionnaireCriterionConfiguration : IEntityTypeConfiguration<QuestionnaireCriterion>
{
    public void Configure(EntityTypeBuilder<QuestionnaireCriterion> b)
    {
        b.ToTable("QuestionnaireCriteria", t => t.HasCheckConstraint("CK_QuestionnaireCriteria_DisplayOrder", "\"DisplayOrder\" >= 0"));
        b.HasKey(x => x.Id);
        b.HasAlternateKey(x => new { x.QuestionnaireId, x.CriterionId });
        b.HasIndex(x => new { x.QuestionnaireId, x.DisplayOrder }).IsUnique();
        b.HasOne(x => x.Criterion).WithMany().HasForeignKey(x => x.CriterionId).OnDelete(DeleteBehavior.Restrict);
    }
}
