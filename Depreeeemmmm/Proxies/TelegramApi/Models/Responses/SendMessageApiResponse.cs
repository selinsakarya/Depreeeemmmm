using System.Text.Json.Serialization;

namespace Depreeeemmmm.Proxies.TelegramApi.Models.Responses;

public class SendMessageApiResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }
}