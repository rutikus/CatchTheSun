namespace CatchTheSun.Core.Entities;

public sealed class InsolationComparisonPoint
{
    public Guid Id { get; set; }
    public Guid InsolationComparisonId { get; set; }
    public string SensorId { get; set; } = string.Empty;
    public double ReferenceValue { get; set; }
    public double TestValue { get; set; }
    public double AbsoluteError { get; set; }
    public double? RelativeError { get; set; }
    public InsolationComparison Comparison { get; set; } = null!;
}
