namespace Depreeeemmmm.Proxies.AfadProxy;

public class AfadProxy : IAfadProxy
{
    private readonly HttpClient _httpClient;
    
    public AfadProxy(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}