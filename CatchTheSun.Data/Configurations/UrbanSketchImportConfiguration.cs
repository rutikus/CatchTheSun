using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public sealed class UrbanSketchImportConfiguration : IEntityTypeConfiguration<UrbanSketchImport>
{
    public void Configure(EntityTypeBuilder<UrbanSketchImport> b)
    {
        b.ToTable("UrbanSketchImports");
        b.HasKey(x => x.Id);
        b.Property(x => x.SourceCsv).IsRequired();
        b.Property(x => x.ImportedAt).IsRequired();
        b.HasOne(x => x.Variant).WithMany(x => x.UrbanSketchImports)
            .HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Measurements).WithOne(x => x.Import)
            .HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Cascade);
    }
}
