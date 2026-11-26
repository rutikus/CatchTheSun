namespace CatchTheSun.Core.Sun.Models;

public sealed record DirectSunImportResult(
    IReadOnlyList<DirectSunMeasurement> Measurements,
    string SourceCsv,
    int ProcessedRecordCount,
    int FeatureCount,
    double MinimumDirectSunHours,
    double MaximumDirectSunHours);
