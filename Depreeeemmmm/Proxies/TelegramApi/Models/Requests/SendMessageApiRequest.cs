using System.Text.Json.Serialization;

namespace Depreeeemmmm.Proxies.TelegramApi.Models.Requests;

public class SendMessageApiRequest
{
    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }
}