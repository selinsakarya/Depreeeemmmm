using System.Text.Json.Serialization;

namespace Depreeeemmmm.Proxies.AfadApiProxy.Models.Responses;

public class QueryEventApiResponse
{
    [JsonPropertyName("eventID")]
    public string EventId { get; set; }

    [JsonPropertyName("latitude")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double Latitude { get; set; }
    
    [JsonPropertyName("longitude")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double Longitude { get; set; }
    
    [JsonPropertyName("depth")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double Depth { get; set; }

    [JsonPropertyName("magnitude")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double Magnitude { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("location")]
    public string Location { get; set; }
}