using MassTransit;

namespace Depreeeemmmm.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(
                    new Uri(configuration["RabbitMQ:Host"]),
                    h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"]);
                        h.Password(configuration["RabbitMQ:Password"]);
                    });

#if DEBUG
                cfg.UseConcurrencyLimit(1);
#endif
                cfg.PrefetchCount = 16;
                cfg.UseConcurrencyLimit(16);

                cfg.UseRetry(r =>
                {
                    r.Incremental(
                        20,
                        TimeSpan.FromMilliseconds(50),
                        TimeSpan.FromMilliseconds(1000));
                    r.Ignore<ApplicationException>();
                });
                    
                
            });
        });

        return services;
    }
}
