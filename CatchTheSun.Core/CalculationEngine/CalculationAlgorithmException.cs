namespace CatchTheSun.Core.CalculationEngine;

public sealed class CalculationAlgorithmException : Exception
{
    public string Code { get; }

    public CalculationAlgorithmException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
