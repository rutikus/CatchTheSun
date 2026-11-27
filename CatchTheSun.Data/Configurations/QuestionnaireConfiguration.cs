using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public sealed class QuestionnaireConfiguration : IEntityTypeConfiguration<Questionnaire>
{
    public void Configure(EntityTypeBuilder<Questionnaire> b)
    {
        b.ToTable("Questionnaires", t =>
        {
            t.HasCheckConstraint("CK_Questionnaires_Status", "\"Status\" IN ('Draft','Published','Closed')");
            t.HasCheckConstraint("CK_Questionnaires_PublicationDates", "(\"Status\" = 'Draft' AND \"PublishedAt\" IS NULL AND \"ClosedAt\" IS NULL) OR (\"Status\" = 'Published' AND \"PublishedAt\" IS NOT NULL AND \"ClosedAt\" IS NULL) OR (\"Status\" = 'Closed' AND \"PublishedAt\" IS NOT NULL AND \"ClosedAt\" IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => x.Status);
        b.HasMany(x => x.Criteria).WithOne(x => x.Questionnaire).HasForeignKey(x => x.QuestionnaireId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Responses).WithOne(x => x.Questionnaire).HasForeignKey(x => x.QuestionnaireId).OnDelete(DeleteBehavior.Cascade);
    }
}
