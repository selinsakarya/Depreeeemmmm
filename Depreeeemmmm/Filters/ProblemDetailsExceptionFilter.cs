using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Depreeeemmmm.Filters;

public class ProblemDetailsExceptionFilter : IActionFilter, IOrderedFilter
{
    public int Order { get; } = int.MaxValue - 10;

    public void OnActionExecuting(ActionExecutingContext context) { }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception is ProblemDetailsException exception)
        {
            ProblemDetails problemDetails = exception.Value;

            string? localizedDetail = ErrorMessageProvider.Get(problemDetails.Type);

            if (string.IsNullOrWhiteSpace(localizedDetail))
            {
                ILogger<ProblemDetailsExceptionFilter> logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ProblemDetailsExceptionFilter>>();
              
                logger.LogError("Resource key could not be mapped. ResourceKey: {ResourceKey}", problemDetails.Type);   
                
                problemDetails.Detail = "Şu anda işleminizi gerçekleştiremiyoruz. Lütfen daha sonra tekrar deneyin.";
            }
            else
            {
                problemDetails.Detail = localizedDetail;
            }
            
            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status,
            };

            context.ExceptionHandled = true;
        }
    }
}

public static class ErrorMessageProvider
{
    private static readonly Dictionary<string, string> _messages;

    static ErrorMessageProvider()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "error-messages.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            _messages = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        else
        {
            _messages = new Dictionary<string, string>();
        }
    }

    public static string? Get(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return null;

        return _messages.TryGetValue(type, out var message) ? message : null;
    }
}
