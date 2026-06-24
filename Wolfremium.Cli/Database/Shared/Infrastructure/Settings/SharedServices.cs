using Microsoft.Extensions.DependencyInjection;
using Wolfremium.Cli.Database.Shared.Domain.Ports;
using Wolfremium.Cli.Database.Shared.Infrastructure;

namespace Wolfremium.Cli.Database.Shared.Infrastructure.Settings;

public static class SharedServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        return services;
    }
}
