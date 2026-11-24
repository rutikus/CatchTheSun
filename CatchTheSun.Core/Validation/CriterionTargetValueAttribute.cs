using System.ComponentModel.DataAnnotations;

namespace CatchTheSun.Core.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CriterionTargetValueAttribute : ValidationAttribute
{
    private const decimal Maximum = 99999999999999.999999m;

    public CriterionTargetValueAttribute() : base("TargetValue must fit numeric(20,6).") { }

    public override bool IsValid(object? value) => value switch
    {
        null => true,
        decimal number => number >= -Maximum && number <= Maximum && number == decimal.Round(number, 6),
        _ => false
    };
}
