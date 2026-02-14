using System.Text;
using System.Text.Json;
using Depreeeemmmm.Extensions;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Responses;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Depreeeemmmm.Proxies.AfadApiProxy;

public class AfadApiProxy : IAfadApiProxy
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AfadApiProxy> _logger;

    public AfadApiProxy(
        HttpClient httpClient,
        ILogger<AfadApiProxy> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProxyResponse<List<QueryEventApiResponse>>> QueryEvents(QueryEventApiRequest request, CancellationToken cancellationToken = default)
    {
        string uri = $"/apiv2/event/filter?{request.ToQueryString()}";

        string requestBody = JsonSerializer.Serialize(request);

        HttpContent httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");

        HttpResponseMessage responseMessage = await _httpClient.PostAsync(uri, httpContent, cancellationToken);

        ProxyResponse<List<QueryEventApiResponse>> proxyResponse = new ProxyResponse<List<QueryEventApiResponse>>();

        string responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        if (responseMessage.IsSuccessStatusCode is false)
        {
            _logger.LogError("Afad - QueryEvents api call failed. StatusCode: {StatusCode}, Content: {Content}", responseMessage.StatusCode, responseContent);

            ErrorResponse errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent)!;

            proxyResponse.ProblemDetails = new ProblemDetails
            {
                Type = errorResponse.Exception,
                Title = errorResponse.Exception,
                Status = (int)responseMessage.StatusCode,
                Detail = $"{errorResponse.Error} {errorResponse.Message}"
            };

            return proxyResponse;
        }

        proxyResponse.Data = JsonSerializer.Deserialize<List<QueryEventApiResponse>>(responseContent)!;

        return proxyResponse;
    }
}