using Depreeeemmmm.Proxies;
using Depreeeemmmm.Proxies.AfadApiProxy;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Requests;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Responses;
using Quartz;

namespace Depreeeemmmm.Jobs;

public class EarthquakeSynchronizer : IJob
{
    private readonly ILogger<EarthquakeSynchronizer> _logger;
    private readonly IAfadApiProxy _afadApiProxy;

    public EarthquakeSynchronizer(
        ILogger<EarthquakeSynchronizer> logger, 
        IAfadApiProxy afadApiProxy)
    {
        _logger = logger;
        _afadApiProxy = afadApiProxy;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("EarthquakeSynchronizer is started");

        DateTime now = DateTime.UtcNow;
        
        DateTime anHourAgo = now.AddHours(-1);
        
        DateTime start = anHourAgo;
        
        DateTime end = now;
        
        QueryEventApiRequest queryEventApiRequest = new QueryEventApiRequest
        {
            Start = start.ToString("yyyy-MM-ddTHH:mm:ss"),
            End = end.ToString("yyyy-MM-ddTHH:mm:ss"),
            OrderBy = "timedesc"
        };

       ProxyResponse<List<QueryEventApiResponse>> queryEventsProxyResponse = await _afadApiProxy.QueryEvents(queryEventApiRequest);
        
        _logger.LogInformation("EarthquakeSynchronizer is finished");
    }
}