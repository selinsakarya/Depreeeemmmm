using Depreeeemmmm.Data;
using Quartz;

namespace Depreeeemmmm.Jobs;

public class EarthquakeSynchronizer : IJob
{
    private readonly ILogger<EarthquakeSynchronizer> _logger;
    private readonly DepremDbContext _depremDbContext;

    public EarthquakeSynchronizer(
        ILogger<EarthquakeSynchronizer> logger,
        DepremDbContext depremDbContext)
    {
        _logger = logger;
        _depremDbContext = depremDbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("EarthquakeSynchronizer is started");

        _logger.LogInformation("EarthquakeSynchronizer is finished");
    }
}