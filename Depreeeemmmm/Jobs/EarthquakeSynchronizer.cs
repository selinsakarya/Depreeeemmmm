using Depreeeemmmm.Proxies.AfadProxy;
using Quartz;

namespace Depreeeemmmm.Jobs;

public class EarthquakeSynchronizer : IJob
{
    private readonly ILogger<EarthquakeSynchronizer> _logger;
    private readonly IAfadProxy _afadProxy;

    public EarthquakeSynchronizer(
        ILogger<EarthquakeSynchronizer> logger, 
        IAfadProxy afadProxy)
    {
        _logger = logger;
        _afadProxy = afadProxy;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("EarthquakeSynchronizer is started");
        
        _logger.LogInformation("EarthquakeSynchronizer is finished");
    }
}