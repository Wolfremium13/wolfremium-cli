using Microsoft.Extensions.DependencyInjection;
using Wolfremium.Cli.Database.Checker.Check.Application.Contracts;
using Wolfremium.Cli.Database.Checker.Check.Application.UseCases;
using Wolfremium.Cli.Database.Checker.Check.Domain.Ports;
using Wolfremium.Cli.Database.Checker.Check.Infrastructure.Postgres;

namespace Wolfremium.Cli.Database.Checker.Check.Infrastructure.Settings;

public static class CheckerServiceCollectionExtensions
{
    public static IServiceCollection AddCheckerServices(this IServiceCollection services)
    {
        services.AddScoped<IConnectionCheckerService, PostgresConnectionCheckerService>();
        services.AddScoped<ICheckConnection, CheckConnectionUseCase>();
        return services;
    }
}
