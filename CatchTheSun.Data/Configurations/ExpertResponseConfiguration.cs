using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public sealed class ExpertResponseConfiguration : IEntityTypeConfiguration<ExpertResponse>
{
    public void Configure(EntityTypeBuilder<ExpertResponse> b)
    {
        b.ToTable("ExpertResponses", t => t.HasCheckConstraint("CK_ExpertResponses_Status", "\"Status\" = 'Submitted'"));
        b.HasKey(x => x.Id);
        b.HasAlternateKey(x => new { x.Id, x.QuestionnaireId });
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.SubmittedAt).IsRequired();
        b.HasIndex(x => new { x.QuestionnaireId, x.ExpertId }).IsUnique();
        b.HasOne(x => x.Expert).WithMany(x => x.Responses).HasForeignKey(x => x.ExpertId).OnDelete(DeleteBehavior.Cascade);
    }
}
