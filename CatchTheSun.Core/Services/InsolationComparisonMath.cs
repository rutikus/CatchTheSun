namespace CatchTheSun.Core.Services;

public static class InsolationComparisonMath
{
    public static (double Absolute, double? Relative) Errors(double reference, double test)
    {
        var absolute = Math.Abs(test - reference);
        return (absolute, reference == 0d ? null : absolute / Math.Abs(reference) * 100d);
    }

    public static (double? Mae, double? Rmse, double? Mape) Aggregate(IReadOnlyCollection<(double Absolute, double? Relative)> errors)
    {
        if (errors.Count == 0) return (null, null, null);
        var mae = errors.Average(x => x.Absolute);
        var rmse = Math.Sqrt(errors.Average(x => x.Absolute * x.Absolute));
        var relative = errors.Where(x => x.Relative.HasValue).Select(x => x.Relative!.Value).ToArray();
        return (mae, rmse, relative.Length == 0 ? null : relative.Average());
    }
}
