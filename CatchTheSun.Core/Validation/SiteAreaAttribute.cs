using System.ComponentModel.DataAnnotations;

namespace CatchTheSun.Core.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SiteAreaAttribute : ValidationAttribute
{
    public const decimal Minimum = 0.0001m;
    public const decimal Maximum = 99999999999999.9999m;

    public SiteAreaAttribute()
        : base("Area must be between 0.0001 and 99999999999999.9999.")
    {
    }

    public override bool IsValid(object? value) => value switch
    {
        null => true,
        decimal area => area >= Minimum && area <= Maximum,
        _ => false
    };
}
