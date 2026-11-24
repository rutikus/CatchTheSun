using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CatchTheSun.Core.DTOs;
using Xunit;

namespace CatchTheSun.Tests;

public class SiteAreaValidationTests
{
    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    public void CreateDtoAcceptsAreaBoundariesRegardlessOfCulture(string cultureName)
    {
        WithCulture(cultureName, () =>
        {
            AssertValid(new CreateSiteDto { Name = "Site", Location = "Location", Area = 0.0001m });
            AssertValid(new CreateSiteDto { Name = "Site", Location = "Location", Area = 99999999999999.9999m });
        });
    }

    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    public void UpdateDtoRejectsValuesOutsideAreaBoundariesRegardlessOfCulture(string cultureName)
    {
        WithCulture(cultureName, () =>
        {
            AssertInvalid(new UpdateSiteDto { Name = "Site", Location = "Location", Area = 0m });
            AssertInvalid(new UpdateSiteDto { Name = "Site", Location = "Location", Area = 100000000000000m });
        });
    }

    private static void AssertValid(object dto) =>
        Assert.Empty(Validate(dto));

    private static void AssertInvalid(object dto) =>
        Assert.Contains(Validate(dto), error => error.MemberNames.Contains("Area"));

    private static IReadOnlyList<ValidationResult> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    private static void WithCulture(string cultureName, Action action)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
