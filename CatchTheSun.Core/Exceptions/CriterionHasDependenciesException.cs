namespace CatchTheSun.Core.Exceptions;

public sealed class CriterionHasDependenciesException(Guid criterionId, Exception? innerException = null)
    : Exception($"Criterion '{criterionId}' is referenced by existing data.", innerException);
