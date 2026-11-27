namespace CatchTheSun.Core.Entities;

public sealed class InsolationComparison
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid? ReferenceDirectSunImportId { get; set; }
    public Guid? ReferenceUrbanSketchImportId { get; set; }
    public Guid? TestDirectSunImportId { get; set; }
    public Guid? TestUrbanSketchImportId { get; set; }
    public double? Mae { get; set; }
    public double? Rmse { get; set; }
    public double? Mape { get; set; }
    public int ComparedPoints { get; set; }
    public int ReferencePoints { get; set; }
    public int TestPoints { get; set; }
    public int UnmatchedReferencePoints { get; set; }
    public int UnmatchedTestPoints { get; set; }
    public DateTime CreatedAt { get; set; }
    public Variant Variant { get; set; } = null!;
    public DirectSunImport? ReferenceDirectSunImport { get; set; }
    public UrbanSketchImport? ReferenceUrbanSketchImport { get; set; }
    public DirectSunImport? TestDirectSunImport { get; set; }
    public UrbanSketchImport? TestUrbanSketchImport { get; set; }
    public ICollection<InsolationComparisonPoint> Points { get; set; } = new List<InsolationComparisonPoint>();
}
