using System.Net.Http.Headers;
using Depreeeemmmm.Constants;
using Depreeeemmmm.Extensions;
using Depreeeemmmm.Jobs;
using Depreeeemmmm.Proxies.AfadProxy;
using Quartz;
using Serilog;
using Serilog.Events;

namespace Depreeeemmmm;

internal static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        
        builder.Host.UseSerilog((context, cfg) =>
        {
            cfg
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Host", Environment.MachineName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Enrich.WithProperty("Cloud", context.Configuration["CloudProvider"])
                .Enrich.WithProperty("BuildId", context.Configuration["BuildId"])
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .ReadFrom.Configuration(context.Configuration);
        });
        
        builder.Services.AddQuartz(q =>
        {
            q.ScheduleJob<EarthquakeSynchronizer>(trigger => trigger
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(1)
                    .RepeatForever()));
        });
        
        builder.Services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        
        builder.Services.AddHttpClient<IAfadProxy, AfadProxy>(cfg =>
            {
                cfg.BaseAddress = new Uri(builder.Configuration["Afad:Url"]!);
                cfg.DefaultRequestHeaders.Add(HeaderKeys.UserAgent, AppConstants.ApplicationName);
                cfg.DefaultRequestHeaders.Add(HeaderKeys.Channel, AppConstants.ApplicationName);
                cfg.DefaultRequestHeaders.Add(HeaderKeys.ClientId, AppConstants.ApplicationName);
                cfg.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpRetryPolicyHandler()
            .AddCircuitBreakerPolicy(200, TimeSpan.FromSeconds(30));

        WebApplication app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}