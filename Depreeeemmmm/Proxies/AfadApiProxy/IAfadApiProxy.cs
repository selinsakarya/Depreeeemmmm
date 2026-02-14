using Depreeeemmmm.Proxies.AfadApiProxy.Models.Requests;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Responses;

namespace Depreeeemmmm.Proxies.AfadApiProxy;

public interface IAfadApiProxy
{
    Task<ProxyResponse<List<QueryEventApiResponse>>> QueryEvents(QueryEventApiRequest request, CancellationToken cancellationToken = default);
}