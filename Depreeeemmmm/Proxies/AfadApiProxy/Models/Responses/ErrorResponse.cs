using System.Text.Json.Serialization;

namespace Depreeeemmmm.Proxies.AfadApiProxy.Models.Responses;

public class ErrorResponse
{
    [JsonPropertyName("exception")]
    public string Exception { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}