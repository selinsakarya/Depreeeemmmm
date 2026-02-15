using System.Text.Json;
using Depreeeemmmm.Extensions;
using Depreeeemmmm.Proxies.TelegramApi.Models.Requests;
using Depreeeemmmm.Proxies.TelegramApi.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using ErrorResponse = Depreeeemmmm.Proxies.TelegramApi.Models.Responses.ErrorResponse;

namespace Depreeeemmmm.Proxies.TelegramApi;

public class TelegramApiProxy : ITelegramApiProxy
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramApiProxy> _logger;

    public TelegramApiProxy(HttpClient httpClient, 
        ILogger<TelegramApiProxy> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProxyResponse<SendMessageApiResponse>> SendMessage(SendMessageApiRequest request, CancellationToken cancellationToken = default)
    {
        string uri = $"sendMessage?{request.ToQueryString()}";

        HttpResponseMessage responseMessage = await _httpClient.GetAsync(uri, cancellationToken);

        ProxyResponse<SendMessageApiResponse> proxyResponse = new ProxyResponse<SendMessageApiResponse>();

        string responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        if (responseMessage.IsSuccessStatusCode is false)
        {
            _logger.LogError("Telegram - SendMessage api call failed. StatusCode: {StatusCode}, Content: {Content}", responseMessage.StatusCode, responseContent);
            
            ErrorResponse errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent)!;
            
            proxyResponse.ProblemDetails = new ProblemDetails
            {
                Type = errorResponse.ErrorCode.ToString(),
                Title = errorResponse.ErrorCode.ToString(),
                Status = (int)responseMessage.StatusCode,
                Detail = $"{errorResponse.Description}"
            };

            return proxyResponse;
        }

        proxyResponse.Data = JsonSerializer.Deserialize<SendMessageApiResponse>(responseContent)!;

        return proxyResponse;

    }
}