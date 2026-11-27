namespace CatchTheSun.Core.Entities;

/// <summary>Normalized UrbanSketch-source CSV row with its original row retained for audit.</summary>
public sealed class UrbanSketchMeasurementRecord
{
    public Guid Id { get; set; }
    public Guid ImportId { get; set; }
    public string SensorId { get; set; } = string.Empty;
    public Guid FeatureId { get; set; }
    public string RecreationalType { get; set; } = string.Empty;
    public double XMetres { get; set; }
    public double YMetres { get; set; }
    public double ZMetres { get; set; }
    public double HeightAboveZoneMetres { get; set; }
    public double GridSpacingMetres { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double DirectSunHoursTestDay { get; set; }
    public int SunlitSamples { get; set; }
    public int SampleCountDaylight { get; set; }
    public string RawRow { get; set; } = string.Empty;
    public UrbanSketchImport Import { get; set; } = null!;
}
