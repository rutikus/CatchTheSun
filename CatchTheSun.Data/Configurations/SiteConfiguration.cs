using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("Sites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Area).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasMany(x => x.Variants).WithOne(x => x.Site).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Calculations).WithOne(x => x.Site).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
    }
}
