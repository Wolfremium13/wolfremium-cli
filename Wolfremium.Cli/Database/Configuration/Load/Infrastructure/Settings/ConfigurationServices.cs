using Microsoft.Extensions.DependencyInjection;
using Wolfremium.Cli.Database.Configuration.Load.Application.Contracts;
using Wolfremium.Cli.Database.Configuration.Load.Application.UseCases;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Ports;
using Wolfremium.Cli.Database.Configuration.Load.Infrastructure.Postgres;

namespace Wolfremium.Cli.Database.Configuration.Load.Infrastructure.Settings;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        services.AddScoped<IConfigurationLoader, EnvironmentConfigurationLoader>();
        services.AddScoped<ILoadConfiguration, LoadConfigurationUseCase>();
        return services;
    }
}
