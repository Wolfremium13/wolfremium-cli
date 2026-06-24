using Microsoft.Extensions.DependencyInjection;
using Wolfremium.Cli.UserInterface;
using Wolfremium.Cli.UserInterface.Components;

namespace Wolfremium.Cli.UserInterface.Settings;

public static class UserInterfaceServiceCollectionExtensions
{
    public static IServiceCollection AddUserInterfaceServices(this IServiceCollection services)
    {
        services.AddTransient<HeaderComponent>();
        services.AddTransient<MainMenuComponent>();
        services.AddTransient<BackupWorkflowComponent>();
        services.AddTransient<ConfigureSettingsComponent>();
        services.AddTransient<TestConnectionComponent>();
        services.AddTransient<CommandLineConsoleApplication>();
        return services;
    }
}
