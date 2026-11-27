using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public sealed class InsolationComparisonConfiguration : IEntityTypeConfiguration<InsolationComparison>
{
    public void Configure(EntityTypeBuilder<InsolationComparison> b)
    {
        b.ToTable("InsolationComparisons", t =>
        {
            t.HasCheckConstraint("CK_InsolationComparisons_ReferenceSource", "(\"ReferenceDirectSunImportId\" IS NOT NULL AND \"ReferenceUrbanSketchImportId\" IS NULL) OR (\"ReferenceDirectSunImportId\" IS NULL AND \"ReferenceUrbanSketchImportId\" IS NOT NULL)");
            t.HasCheckConstraint("CK_InsolationComparisons_TestSource", "(\"TestDirectSunImportId\" IS NOT NULL AND \"TestUrbanSketchImportId\" IS NULL) OR (\"TestDirectSunImportId\" IS NULL AND \"TestUrbanSketchImportId\" IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.VariantId, x.CreatedAt });
        b.HasIndex(x => x.ReferenceDirectSunImportId);
        b.HasIndex(x => x.ReferenceUrbanSketchImportId);
        b.HasIndex(x => x.TestDirectSunImportId);
        b.HasIndex(x => x.TestUrbanSketchImportId);
        b.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ReferenceDirectSunImport).WithMany().HasForeignKey(x => x.ReferenceDirectSunImportId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ReferenceUrbanSketchImport).WithMany().HasForeignKey(x => x.ReferenceUrbanSketchImportId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.TestDirectSunImport).WithMany().HasForeignKey(x => x.TestDirectSunImportId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.TestUrbanSketchImport).WithMany().HasForeignKey(x => x.TestUrbanSketchImportId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Points).WithOne(x => x.Comparison).HasForeignKey(x => x.InsolationComparisonId).OnDelete(DeleteBehavior.Cascade);
    }
}
