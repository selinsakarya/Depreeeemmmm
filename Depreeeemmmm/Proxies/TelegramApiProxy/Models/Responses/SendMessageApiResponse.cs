using System.Text.Json.Serialization;

namespace Depreeeemmmm.Proxies.TelegramApiProxy.Models.Responses;

public class SendMessageApiResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }
}