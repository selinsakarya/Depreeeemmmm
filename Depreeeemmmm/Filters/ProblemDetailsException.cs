using Microsoft.AspNetCore.Mvc;

namespace Depreeeemmmm.Filters;

public class ProblemDetailsException : Exception
{
    public ProblemDetailsException(ProblemDetails value)
    {
        Value = value;
    }

    public ProblemDetails Value { get; }
}