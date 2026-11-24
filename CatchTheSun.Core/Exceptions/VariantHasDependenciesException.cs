namespace CatchTheSun.Core.Exceptions;

public sealed class VariantHasDependenciesException(Guid variantId, Exception? innerException = null)
    : Exception($"Variant '{variantId}' cannot be deleted because it has dependent records.", innerException)
{
    public Guid VariantId { get; } = variantId;
}
