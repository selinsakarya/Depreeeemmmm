using Polly;
using Polly.Extensions.Http;

namespace Depreeeemmmm.Extensions;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddHttpRetryPolicyHandler(this IHttpClientBuilder builder, int retryCount = 5, Func<int, TimeSpan>? retryAttempt = null)
    {
        Random jitter = new Random();

        retryAttempt ??= (attempt) => TimeSpan.FromMilliseconds(Math.Pow(2, attempt))
                                      + TimeSpan.FromMilliseconds(jitter.Next(0, 20));

        return builder.AddPolicyHandler((serviceProvider, _) =>
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(retryCount, retryAttempt, onRetryAsync: async (outcome, timespan, attempt, _) =>
                {
                    ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                    ILogger logger = loggerFactory.CreateLogger("HttpClientBuilderExtensions");

                    string httpResponse = null;
                    int? httpStatusCode = null;

                    if (outcome?.Result != null)
                    {
                        httpResponse = await outcome.Result.Content.ReadAsStringAsync();
                        httpStatusCode = (int)outcome.Result.StatusCode;
                    }

                    logger.LogWarning(outcome?.Exception, $"Delaying for {timespan.TotalMilliseconds} ms, then making retry {attempt} HttpResponse:{httpResponse} HttpStatusCode:{httpStatusCode}");
                });
        });
    }

    public static IHttpClientBuilder AddCircuitBreakerPolicy(this IHttpClientBuilder builder, int handledEventsAllowedBeforeBreaking, TimeSpan durationOfBreak)
    {
        return builder.AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking, durationOfBreak));
    }
}