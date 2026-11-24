using System.ComponentModel.DataAnnotations;

namespace CatchTheSun.Core.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CriterionNumericValueAttribute : ValidationAttribute
{
    private const decimal Maximum = 99999999999999.999999m;
    public CriterionNumericValueAttribute() : base("Value must fit PostgreSQL numeric(20,6).") { }
    public override bool IsValid(object? value) => value is decimal number && number >= -Maximum && number <= Maximum && number == decimal.Round(number, 6);
}
