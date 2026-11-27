using CatchTheSun.Core.Sun;
using Xunit;

namespace CatchTheSun.Tests;

public sealed class DirectSunCsvParserTests
{
    private const string Header = "sensor_id,feature_id,recreational_type,x_m,y_m,z_m,height_above_zone_m,grid_spacing_m,longitude,latitude,direct_sun_hours_test_day,sunlit_samples,sample_count_daylight";
    private static string Row(int i, string? sensor = null, string? hours = null) =>
        $"{sensor ?? Guid.NewGuid().ToString("D")},{Guid.Parse("10000000-0000-0000-0000-000000000001")},park,{i}.5,2,0,1.5,0.5,60.1,56.8,{hours ?? (i % 25).ToString(System.Globalization.CultureInfo.InvariantCulture)},2,4";

    [Fact]
    public void ParsesDynamicRecordCountAndRetainsOriginalCsv()
    {
        var csv = Header + "\n" + string.Join("\n", Enumerable.Range(0, 347).Select(i => Row(i)));
        var result = new DirectSunCsvParser().Parse(csv);
        Assert.Equal(347, result.ProcessedRecordCount);
        Assert.Equal(1, result.FeatureCount);
        Assert.Equal(0, result.MinimumDirectSunHours);
        Assert.Equal(24, result.MaximumDirectSunHours);
        Assert.Equal(csv, result.SourceCsv);
        Assert.Equal(347, result.Measurements.Count);
    }

    [Fact]
    public void PreservesLadybugSensorIdSuffix()
    {
        const string sensorId = "092a4b73-9ced-4856-89fe-740d0bd81ca4-0-0000";
        var result = new DirectSunCsvParser().Parse(Header + "\n" + Row(1, sensorId));
        Assert.Equal(sensorId, Assert.Single(result.Measurements).SensorId);
    }

    [Fact]
    public void ParsesProvidedLadybugCsv()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "direct_sun_hours_points.csv");
        var csv = File.ReadAllText(path);
        var result = new DirectSunCsvParser().Parse(csv);
        Assert.Equal(347, result.ProcessedRecordCount);
        Assert.Equal(347, result.Measurements.Select(x => x.SensorId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(8, result.FeatureCount);
        Assert.Equal(1.5, result.MinimumDirectSunHours);
        Assert.Equal(8.5, result.MaximumDirectSunHours);
        Assert.Equal(csv, result.SourceCsv);
        Assert.Equal("092a4b73-9ced-4856-89fe-740d0bd81ca4-0-0000", result.Measurements[0].SensorId);
        Assert.All(result.Measurements, x => Assert.NotEqual(Guid.Empty, x.FeatureId));
    }

    [Theory]
    [InlineData("", "CSV is empty")]
    public void RejectsEmptyFile(string csv, string _) => Assert.Throws<FormatException>(() => new DirectSunCsvParser().Parse(csv));

    [Fact]
    public void RejectsMissingHeader() => Assert.Throws<FormatException>(() => new DirectSunCsvParser().Parse(Header.Replace(",latitude", "") + "\n" + Row(1)));
    [Fact]
    public void RejectsMalformedFeatureUuid() => Assert.Throws<FormatException>(() => new DirectSunCsvParser().Parse(Header + "\n" + Row(1).Replace("10000000-0000-0000-0000-000000000001", "not-a-uuid")));
    [Fact]
    public void RejectsDuplicateSensorIds() => Assert.Throws<FormatException>(() => new DirectSunCsvParser().Parse(Header + "\n" + Row(1, "same-sensor") + "\n" + Row(2, "same-sensor")));
    [Fact]
    public void RejectsEmptySensorId() => Assert.Throws<FormatException>(() => new DirectSunCsvParser().Parse(Header + "\n" + Row(1, "")));
    [Fact]
    public void RejectsMissingRequiredValue() => Assert.Throws<FormatException>(() => new DirectSunCsvParser().Parse(Header + "\n" + Row(1).Replace(",park,", ",,")));
    [Fact]
    public void RejectsOutOfRangeSunHours() => Assert.Throws<FormatException>(() => new DirectSunCsvParser().Parse(Header + "\n" + Row(1, hours: "24.1")));
    [Fact]
    public void RejectsNonNumericCoordinate() => Assert.Throws<FormatException>(() => new DirectSunCsvParser().Parse(Header + "\n" + Row(1).Replace(",60.1,56.8,", ",not-a-number,56.8,")));
    [Fact]
    public void RejectsInconsistentSampleCounts() => Assert.Throws<FormatException>(() => new DirectSunCsvParser().Parse(Header + "\n" + Row(1).Replace(",2,4", ",5,4")));
    [Fact]
    public void RejectsWrongColumnCount() => Assert.Throws<FormatException>(() => new DirectSunCsvParser().Parse(Header + "\n" + "only,three,columns"));
}
