namespace CatchTheSun.Core.Exceptions;

public sealed class DuplicateCriterionNameException(string name)
    : Exception($"A criterion named '{name}' already exists.");
