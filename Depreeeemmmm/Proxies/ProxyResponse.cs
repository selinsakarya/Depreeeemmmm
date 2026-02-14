using Microsoft.AspNetCore.Mvc;

namespace Depreeeemmmm.Proxies;

public class ProxyResponse<T> : ProxyResponse
{
    public T Data { get; set; }
}

public class ProxyResponse
{
    public ProblemDetails ProblemDetails { get; set; }

    public bool HasError => ProblemDetails != null;
}