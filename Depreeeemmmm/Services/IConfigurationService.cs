using Depreeeemmmm.Data.Entities;

namespace Depreeeemmmm.Services;

public interface IConfigurationService
{
    Task<Configuration?> GetConfiguration(string key);
}