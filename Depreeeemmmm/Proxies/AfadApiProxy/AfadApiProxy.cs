using System.Text.Json;
using Depreeeemmmm.Extensions;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Responses;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Requests;

namespace Depreeeemmmm.Proxies.AfadApiProxy;

public class AfadApiProxy : IAfadApiProxy
{
    private readonly HttpClient _httpClient;

    public AfadApiProxy(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<QueryEventApiResponse>> QueryEvents(QueryEventApiRequest request, CancellationToken cancellationToken = default)
    {
        throw new Exception("test");
        
        string uri = $"/apiv2/event/filter?{request.ToQueryString()}";

        HttpResponseMessage responseMessage = await _httpClient.GetAsync(uri, cancellationToken);

        string responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        if (responseMessage.IsSuccessStatusCode is false)
        {
            throw new Exception($"Afad - QueryEvents api call failed. StatusCode: {responseMessage.StatusCode}, ResponseContent: {responseContent}");
        }

        List<QueryEventApiResponse> response = JsonSerializer.Deserialize<List<QueryEventApiResponse>>(responseContent)!;

        return response;
    }
}