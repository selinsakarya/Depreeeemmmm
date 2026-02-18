using System.Text.Json.Serialization;

namespace Depreeeemmmm.Proxies.TelegramApiProxy.Models.Responses;

public class ErrorResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}