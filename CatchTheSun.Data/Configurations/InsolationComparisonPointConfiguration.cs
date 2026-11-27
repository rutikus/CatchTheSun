using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public sealed class InsolationComparisonPointConfiguration : IEntityTypeConfiguration<InsolationComparisonPoint>
{
    public void Configure(EntityTypeBuilder<InsolationComparisonPoint> b)
    {
        b.ToTable("InsolationComparisonPoints");
        b.HasKey(x => x.Id);
        b.Property(x => x.SensorId).IsRequired();
        b.HasIndex(x => new { x.InsolationComparisonId, x.SensorId }).IsUnique();
    }
}
