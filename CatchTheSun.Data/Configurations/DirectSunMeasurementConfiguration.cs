using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatchTheSun.Data.Configurations;

public sealed class DirectSunMeasurementConfiguration : IEntityTypeConfiguration<DirectSunMeasurementRecord>
{
    public void Configure(EntityTypeBuilder<DirectSunMeasurementRecord> b)
    {
        b.ToTable("DirectSunMeasurements"); b.HasKey(x => x.Id);
        b.Property(x => x.SensorId).IsRequired();
        b.Property(x => x.RecreationalType).IsRequired(); b.Property(x => x.RawRow).IsRequired();
        b.HasIndex(x => new { x.ImportId, x.FeatureId });
        b.HasIndex(x => new { x.ImportId, x.SensorId }).IsUnique();
    }
}
