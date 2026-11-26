namespace CatchTheSun.Core.Entities;

/// <summary>Original uploaded Ladybug CSV and its association with a design variant.</summary>
public sealed class DirectSunImport
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string SourceCsv { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public Variant Variant { get; set; } = null!;
    public ICollection<DirectSunMeasurementRecord> Measurements { get; set; } = new List<DirectSunMeasurementRecord>();
}
