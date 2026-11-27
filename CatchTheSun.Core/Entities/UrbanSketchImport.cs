namespace CatchTheSun.Core.Entities;

/// <summary>Original CSV upload and measurements imported through the UrbanSketch source endpoint.</summary>
public sealed class UrbanSketchImport
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string SourceCsv { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public Variant Variant { get; set; } = null!;
    public ICollection<UrbanSketchMeasurementRecord> Measurements { get; set; } = new List<UrbanSketchMeasurementRecord>();
}
