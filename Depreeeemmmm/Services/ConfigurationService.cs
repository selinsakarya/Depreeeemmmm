using Depreeeemmmm.Constants;
using Depreeeemmmm.Data;
using Depreeeemmmm.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Depreeeemmmm.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IMemoryCache _memoryCache;
    private readonly DepremDbContext _depremDbContext;

    public ConfigurationService(
        IMemoryCache memoryCache, 
        DepremDbContext depremDbContext)
    {
        _memoryCache = memoryCache;
        _depremDbContext = depremDbContext;
    }
    
    public async Task<Configuration?> GetConfiguration(string key)
    {
        List<Configuration> configurations = await GetConfigurations();

        Configuration? configuration = configurations.Find(x => x.Key == key);

        return configuration;
    }

    private async Task<List<Configuration>> GetConfigurations()
    {
        string cacheKey = CacheKeys.Configurations;
        
        List<Configuration>? configurations = _memoryCache.Get<List<Configuration>>(cacheKey);

        if (configurations is not null)
        {
            return configurations;
        }
        
        configurations = await _depremDbContext.Configurations.AsNoTracking().ToListAsync();

        _memoryCache.Set(configurations, cacheKey, TimeSpan.FromMinutes(30));

        return configurations;
    }
}