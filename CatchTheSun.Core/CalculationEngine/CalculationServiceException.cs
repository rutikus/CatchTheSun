namespace CatchTheSun.Core.CalculationEngine;

public enum CalculationServiceErrorKind
{
    InvalidRequest,
    NotFound,
    Validation,
    Conflict
}

public sealed class CalculationServiceException : Exception
{
    public string Code { get; }
    public CalculationServiceErrorKind Kind { get; }

    public CalculationServiceException(
        string code,
        CalculationServiceErrorKind kind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Kind = kind;
    }
}
