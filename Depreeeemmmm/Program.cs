using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Depreeeemmmm.Constants;
using Depreeeemmmm.Data;
using Depreeeemmmm.Extensions;
using Depreeeemmmm.Factories;
using Depreeeemmmm.Filters;
using Depreeeemmmm.Jobs;
using Depreeeemmmm.Proxies.AfadApiProxy;
using Depreeeemmmm.Proxies.TelegramApiProxy;
using Depreeeemmmm.Services;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;
using Serilog.Events;

namespace Depreeeemmmm;

internal abstract class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddControllers(options => options.Filters.Add(new ProblemDetailsExceptionFilter()))
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .AddFluentValidation(fv =>
            {
                fv.DisableDataAnnotationsValidation = true;
                fv.RegisterValidatorsFromAssemblyContaining<Program>();
                fv.ImplicitlyValidateChildProperties = true;
            });        
        builder.Services.AddOpenApi();
        
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
        
        builder.Services.AddSwaggerGen(c =>
        {
            c.CustomSchemaIds(t => t.FullName);

            c.SwaggerDoc("v1", new() { Title = "Depreeeemmmm", Version = "v1" });
        });
        
        builder.Services.AddQuartz(q =>
        {
            q.ScheduleJob<AfadEarthquakeSynchronizer>(trigger => trigger
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(1)
                    .RepeatForever()));
        });
        
        builder.Services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });
        
        builder.Services.AddDbContext<DepremDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DepremDbConnectionString")));
        
        builder.Services.AddDbContext<DepremDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DepremDbConnectionString"), x => x.UseNetTopologySuite()));
        
        builder.Services.AddMassTransit(builder.Configuration);
        
        builder.Services.AddHttpClient<IAfadApiProxy, AfadApiProxy>(cfg =>
            {
                cfg.BaseAddress = new Uri(builder.Configuration["Afad:Url"]!);
                cfg.DefaultRequestHeaders.Add(HeaderKeys.UserAgent, AppConstants.ApplicationName);
                cfg.DefaultRequestHeaders.Add(HeaderKeys.Channel, AppConstants.ApplicationName);
                cfg.DefaultRequestHeaders.Add(HeaderKeys.ClientId, AppConstants.ApplicationName);
                cfg.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpRetryPolicyHandler()
            .AddCircuitBreakerPolicy(200, TimeSpan.FromSeconds(30));

        string? telegramBotUri = $"{ Environment.GetEnvironmentVariable("TELEGRAM_BOT_URI")}";

        builder.Services.AddHttpClient<ITelegramApiProxy, TelegramApiProxy>(cfg =>
            {
                cfg.BaseAddress = new Uri(telegramBotUri!);
                cfg.DefaultRequestHeaders.Add(HeaderKeys.UserAgent, AppConstants.ApplicationName);
                cfg.DefaultRequestHeaders.Add(HeaderKeys.Channel, AppConstants.ApplicationName);
                cfg.DefaultRequestHeaders.Add(HeaderKeys.ClientId, AppConstants.ApplicationName);
                cfg.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpRetryPolicyHandler()
            .AddCircuitBreakerPolicy(200, TimeSpan.FromSeconds(30));

        builder.Services.AddMemoryCache();
        
        builder.Services.AddHostedService<OutboxMessagePublisherHostedService>();
        
        builder.Services.AddScoped<IOutboxMessagePublisherService, OutboxMessagePublisherService>();
        builder.Services.AddScoped<IOutboxMessageFactory, OutboxMessageFactory>();
        builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

        WebApplication app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            
            app.UseSwagger();

            app.UseSwaggerUI();
        
            app.MapGet("/", () => Results.Redirect("/swagger/index.html")).ExcludeFromDescription();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}