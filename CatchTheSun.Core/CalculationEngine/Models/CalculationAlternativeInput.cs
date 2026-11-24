namespace CatchTheSun.Core.CalculationEngine.Models;

public sealed class CalculationAlternativeInput
{
    public Guid VariantId { get; }
    public IReadOnlyList<decimal> Values { get; }

    /// <param name="values">
    /// Criterion values in the same order as the criteria supplied to
    /// <see cref="CalculationInput"/>. CalculationInput canonicalizes both.
    /// </param>
    public CalculationAlternativeInput(Guid variantId, IEnumerable<decimal> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        VariantId = variantId;
        Values = Array.AsReadOnly(values.ToArray());
    }
}
