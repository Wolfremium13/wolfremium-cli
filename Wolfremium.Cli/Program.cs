using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Models;
using Wolfremium.Cli.Database.Backup.Execute.Application.Contracts;
using Wolfremium.Cli.Database.Configuration.Load.Application.Contracts;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Models;
using Wolfremium.Cli.Database.Shared.Domain.Ports;
using Wolfremium.Cli.UserInterface;
using Wolfremium.Cli.Database.Backup.Execute.Infrastructure.Settings;
using Wolfremium.Cli.Database.Checker.Check.Infrastructure.Settings;
using Wolfremium.Cli.Database.Configuration.Load.Infrastructure.Settings;
using Wolfremium.Cli.Database.Shared.Infrastructure.Settings;
using Wolfremium.Cli.UserInterface.Settings;

namespace Wolfremium.Cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddSharedServices();
        services.AddConfigurationServices();
        services.AddCheckerServices();
        services.AddBackupServices();
        services.AddUserInterfaceServices();

        using var serviceProvider = services.BuildServiceProvider();

        var dateTimeProvider = serviceProvider.GetRequiredService<IDateTimeProvider>();
        var loadConfigurationUseCase = serviceProvider.GetRequiredService<ILoadConfiguration>();
        var configurationEither = await loadConfigurationUseCase.ExecuteAsync(new ConfigurationLoadRequest());

        var appConfiguration = configurationEither.Match(
            valid => valid,
            error => throw new InvalidOperationException($"Invalid environment configuration: {error.Message}")
        );

        var settingsEither = BackupSettings.Create(
            appConfiguration.Host,
            appConfiguration.Port,
            appConfiguration.Username,
            appConfiguration.Password,
            appConfiguration.Database,
            appConfiguration.ExportPath,
            dateTimeProvider
        );

        var settings = settingsEither.Match(
            valid => valid,
            error => throw new InvalidOperationException($"Invalid environment configuration: {error.Message}")
        );

        // Register the resolved settings in container for dependency injection to UI components
        services.AddSingleton(settings);
        
        // Rebuild provider to include settings
        using var finalServiceProvider = services.BuildServiceProvider();

        var app = finalServiceProvider.GetRequiredService<CommandLineConsoleApplication>();
        await app.RunAsync();
    }
}