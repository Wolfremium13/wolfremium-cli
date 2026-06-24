using Microsoft.Extensions.DependencyInjection;
using Wolfremium.Cli.Database.Backup.Execute.Application.Contracts;
using Wolfremium.Cli.Database.Backup.Execute.Application.UseCases;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Ports;
using Wolfremium.Cli.Database.Backup.Execute.Infrastructure.Postgres;

namespace Wolfremium.Cli.Database.Backup.Execute.Infrastructure.Settings;

public static class BackupServiceCollectionExtensions
{
    public static IServiceCollection AddBackupServices(this IServiceCollection services)
    {
        services.AddScoped<IDatabaseService, PostgresDatabaseService>();
        services.AddScoped<IBackupWriter, FileBackupWriter>();
        services.AddScoped<IExecuteBackup, ExecuteBackupUseCase>();
        return services;
    }
}
