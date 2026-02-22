using NetTopologySuite.Geometries;
using Depreeeemmmm.Data;
using Depreeeemmmm.Data.Entities;
using Depreeeemmmm.Data.Enums;
using Depreeeemmmm.Factories;
using Depreeeemmmm.Proxies.AfadApiProxy;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Requests;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Responses;
using Events;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Depreeeemmmm.Jobs;

[DisallowConcurrentExecution]
public class AfadEarthquakeSynchronizer : IJob
{
    private readonly ILogger<AfadEarthquakeSynchronizer> _logger;
    private readonly IAfadApiProxy _afadApiProxy;
    private readonly DepremDbContext _depremDbContext;
    private readonly IOutboxMessageFactory _outboxMessageFactory;

    public AfadEarthquakeSynchronizer(
        ILogger<AfadEarthquakeSynchronizer> logger, 
        IAfadApiProxy afadApiProxy, 
        DepremDbContext depremDbContext, 
        IOutboxMessageFactory outboxMessageFactory)
    {
        _logger = logger;
        _afadApiProxy = afadApiProxy;
        _depremDbContext = depremDbContext;
        _outboxMessageFactory = outboxMessageFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("AfadEarthquakeSynchronizer is started.");

        DateTime now = DateTime.UtcNow;
        
        DateTime lastYear = now.AddYears(-1);
        
        DateTime startTime = lastYear;
        
        DateTime endTime = now;

        Earthquake? lastEarthquake = await _depremDbContext.Earthquakes.OrderByDescending(e => e.OccurredAt).FirstOrDefaultAsync(context.CancellationToken);

        if (lastEarthquake is not null)
        {
            startTime = lastEarthquake.OccurredAt;
        }
        
        QueryEventApiRequest queryEventApiRequest = new QueryEventApiRequest
        {
            Start = startTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            End = endTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            OrderBy = "timedesc"
        };

       List<QueryEventApiResponse> queryEventApiResponse = await _afadApiProxy.QueryEvents(queryEventApiRequest);

       if (queryEventApiResponse.Count == 0)
       {
           _logger.LogInformation("No events occured. Start: {StartTime} End: {EndTime}", startTime, endTime);

           return;
       }
       
       List<Earthquake> earthquakes = await _depremDbContext.Earthquakes.AsNoTracking().Where(e => e.OccurredAt >= startTime && e.OccurredAt < endTime).ToListAsync(context.CancellationToken);

       foreach (QueryEventApiResponse @event in queryEventApiResponse)
       {
           bool earthquakeAlreadyExist = earthquakes.Any(e => e.IntegrationReferenceId == @event.EventId);

           if (earthquakeAlreadyExist)
           {
               continue;
           }

           Guid secondaryUniqueId = Guid.NewGuid();
           
           Earthquake earthquake = new Earthquake
           {
               SecondaryUniqueId = secondaryUniqueId,
               Magnitude = @event.Magnitude,
               Depth = @event.Depth,
               Coordinates = new Point(@event.Longitude, @event.Latitude) { SRID = 4326 },
               OccurredAt = @event.Date,
               IntegrationReferenceId = @event.EventId,
               Source = EarthquakeSource.Afad,
               Location = @event.Location,
               CreatedAt = now,
               CreatedBy = nameof(AfadEarthquakeSynchronizer),
               UpdatedAt = now,
               UpdatedBy = nameof(AfadEarthquakeSynchronizer)
           };

           _depremDbContext.Earthquakes.Add(earthquake);

           EarthquakeOccurred earthquakeOccurred = new EarthquakeOccurred
           {
               EarthquakeSecondaryUniqueId = secondaryUniqueId
           };
           
           OutboxMessage earthquakeOccuredOutboxMessage = _outboxMessageFactory.From(earthquakeOccurred, now);
           
           _depremDbContext.OutboxMessages.Add(earthquakeOccuredOutboxMessage);
           
           _logger.LogWarning("An earthquake occurred. EventMagnitude: {EventMagnitude} EventLocation: {EventLocation} Date: {EventDate}", @event.Magnitude, @event.Location, @event.Date);
       }

       await _depremDbContext.SaveChangesAsync(context.CancellationToken);

       _logger.LogInformation("AfadEarthquakeSynchronizer is finished.");
    }
}