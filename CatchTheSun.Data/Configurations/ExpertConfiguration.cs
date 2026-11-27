using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public sealed class ExpertConfiguration : IEntityTypeConfiguration<Expert>
{
    public void Configure(EntityTypeBuilder<Expert> b)
    {
        b.ToTable("Experts"); b.HasKey(x => x.Id);
        b.Property(x => x.AnonymousCode).HasMaxLength(40).IsRequired();
        b.Property(x => x.ExperienceCategory).HasMaxLength(100);
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => x.AnonymousCode).IsUnique();
        b.HasMany(x => x.Responses).WithOne(x => x.Expert).HasForeignKey(x => x.ExpertId).OnDelete(DeleteBehavior.Cascade);
    }
}
