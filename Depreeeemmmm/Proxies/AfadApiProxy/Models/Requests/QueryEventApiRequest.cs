using System.Text.Json.Serialization;

namespace Depreeeemmmm.Proxies.AfadApiProxy.Models.Requests;

public class QueryEventApiRequest
{
    [JsonPropertyName("start")]
    public string Start { get; set; }
 
    [JsonPropertyName("end")]
    public string End { get; set; }

    [JsonPropertyName("orderBy")]
    public string OrderBy { get; set; }
}