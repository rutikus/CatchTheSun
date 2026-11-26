using System.Globalization;
using System.Text;
using CatchTheSun.Core.Sun.Models;

namespace CatchTheSun.Core.Sun;

/// <summary>Parses Ladybug Direct Sun Hours point exports. The measure is test-day hours, not a normative-period total.</summary>
public sealed class DirectSunCsvParser
{
    public static readonly string[] RequiredHeaders = [
        "sensor_id", "feature_id", "recreational_type", "x_m", "y_m", "z_m",
        "height_above_zone_m", "grid_spacing_m", "longitude", "latitude",
        "direct_sun_hours_test_day", "sunlit_samples", "sample_count_daylight"];

    public DirectSunImportResult Parse(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        var rows = ParseRows(csv);
        if (rows.Count == 0) throw new FormatException("CSV is empty.");
        var headers = rows[0].Select(x => x.Trim()).ToArray();
        headers[0] = headers[0].TrimStart('\uFEFF');
        var duplicate = headers.GroupBy(x => x, StringComparer.Ordinal).FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null) throw new FormatException($"Duplicate CSV header '{duplicate.Key}'.");
        var missing = RequiredHeaders.Except(headers, StringComparer.Ordinal).ToArray();
        if (missing.Length > 0) throw new FormatException($"Missing required CSV headers: {string.Join(", ", missing)}.");
        var indexes = RequiredHeaders.Select(h => Array.IndexOf(headers, h)).ToArray();
        var parsed = new List<DirectSunMeasurement>();
        var sensorIds = new HashSet<string>(StringComparer.Ordinal);
        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0])) continue;
            if (row.Count != headers.Length) throw Error(rowIndex + 1, $"expected {headers.Length} columns, found {row.Count}");
            string V(int i) => row[indexes[i]].Trim();
            void Required(int i) { if (string.IsNullOrWhiteSpace(V(i))) throw Error(rowIndex + 1, $"'{RequiredHeaders[i]}' is required"); }
            for (var i = 0; i < RequiredHeaders.Length; i++) Required(i);
            Guid Id(int i) => Guid.TryParse(V(i), out var id) && id != Guid.Empty ? id : throw Error(rowIndex + 1, $"'{RequiredHeaders[i]}' must be a non-empty UUID");
            double Num(int i) => double.TryParse(V(i), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
                ? value : throw Error(rowIndex + 1, $"'{RequiredHeaders[i]}' must be a finite number");
            int Count(int i) => int.TryParse(V(i), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= 0
                ? value : throw Error(rowIndex + 1, $"'{RequiredHeaders[i]}' must be a non-negative integer");
            var sun = Num(10); var longitude = Num(8); var latitude = Num(9);
            var sunlit = Count(11); var daylight = Count(12);
            if (sun < 0 || sun > 24) throw Error(rowIndex + 1, "direct_sun_hours_test_day must be between 0 and 24");
            if (longitude is < -180 or > 180) throw Error(rowIndex + 1, "longitude must be between -180 and 180");
            if (latitude is < -90 or > 90) throw Error(rowIndex + 1, "latitude must be between -90 and 90");
            if (sunlit > daylight) throw Error(rowIndex + 1, "sunlit_samples cannot exceed sample_count_daylight");
            var sensorId = row[indexes[0]];
            if (string.IsNullOrWhiteSpace(sensorId)) throw Error(rowIndex + 1, "'sensor_id' is required");
            if (!sensorIds.Add(sensorId)) throw Error(rowIndex + 1, $"duplicate sensor_id '{sensorId}'");
            parsed.Add(new(sensorId, Id(1), V(2), Num(3), Num(4), Num(5), Num(6), Num(7), longitude, latitude,
                sun, sunlit, daylight, string.Join(",", row.Select(Escape))));
        }
        if (parsed.Count == 0) throw new FormatException("CSV contains no measurement records.");
        return new(parsed, csv, parsed.Count, parsed.Select(x => x.FeatureId).Distinct().Count(),
            parsed.Min(x => x.DirectSunHoursTestDay), parsed.Max(x => x.DirectSunHoursTestDay));
    }

    private static FormatException Error(int line, string message) => new($"CSV line {line}: {message}.");
    private static string Escape(string value) => value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
        ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;

    private static List<List<string>> ParseRows(string csv)
    {
        var rows = new List<List<string>>(); var row = new List<string>(); var field = new StringBuilder(); var quoted = false;
        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false; else field.Append(c);
            }
            else if (c == '"' && field.Length == 0) quoted = true;
            else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                row.Add(field.ToString()); field.Clear(); rows.Add(row); row = [];
            }
            else field.Append(c);
        }
        if (quoted) throw new FormatException("CSV contains an unterminated quoted field.");
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }
        return rows;
    }
}
