using Depreeeemmmm.Data;
using Depreeeemmmm.Data.Entities;
using Depreeeemmmm.Data.Enums;
using Depreeeemmmm.Factories;
using Depreeeemmmm.Proxies;
using Depreeeemmmm.Proxies.AfadApiProxy;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Requests;
using Depreeeemmmm.Proxies.AfadApiProxy.Models.Responses;
using Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Depreeeemmmm.Jobs;

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

       if (queryEventsProxyResponse.HasError)
       {
           ProblemDetails problemDetails = queryEventsProxyResponse.ProblemDetails;

           _logger.LogError($"An error occured while querying events. Status {problemDetails.Status} Title: {problemDetails.Title} Type: {problemDetails.Type} Detail: {problemDetails.Detail}");

           return;
       }
       
       List<QueryEventApiResponse> events = queryEventsProxyResponse.Data;

       if (events.Count == 0)
       {
           _logger.LogInformation($"No events occured. Start: {start} End: {end}");

           return;
       }
       
       List<Earthquake> earthquakes = await _depremDbContext.Earthquakes.AsNoTracking().Where(e => e.OccurredAt >= start && e.OccurredAt < end).ToListAsync();

       foreach (QueryEventApiResponse @event in events)
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
               Latitude = @event.Latitude,
               Longitude = @event.Longitude,
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
           
           _logger.LogWarning($"An earthquake ocurred. Magnitude: {@event.Magnitude} Location: {@event.Location} Date: {@event.Date}");
       }

       await _depremDbContext.SaveChangesAsync(context.CancellationToken);

       _logger.LogInformation("AfadEarthquakeSynchronizer is finished.");
    }
}