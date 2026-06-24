using System;
using System.Threading.Tasks;
using Spectre.Console;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Models;
using Wolfremium.Cli.UserInterface.Components;

namespace Wolfremium.Cli.UserInterface;

public class CommandLineConsoleApplication
{
    private readonly HeaderComponent _headerComponent;
    private readonly MainMenuComponent _mainMenuComponent;
    private readonly BackupWorkflowComponent _backupWorkflowComponent;
    private readonly ConfigureSettingsComponent _configureSettingsComponent;
    private readonly TestConnectionComponent _testConnectionComponent;
    private BackupSettings _settings;

    public CommandLineConsoleApplication(
        HeaderComponent headerComponent,
        MainMenuComponent mainMenuComponent,
        BackupWorkflowComponent backupWorkflowComponent,
        ConfigureSettingsComponent configureSettingsComponent,
        TestConnectionComponent testConnectionComponent,
        BackupSettings settings)
    {
        _headerComponent = headerComponent;
        _mainMenuComponent = mainMenuComponent;
        _backupWorkflowComponent = backupWorkflowComponent;
        _configureSettingsComponent = configureSettingsComponent;
        _testConnectionComponent = testConnectionComponent;
        _settings = settings;
    }

    public async Task RunAsync()
    {
        Console.Title = "Wolfremium CLI - Developer Tools";

        var exit = false;
        while (!exit)
        {
            AnsiConsole.Clear();
            _headerComponent.Render();

            var choice = _mainMenuComponent.ShowMenu();

            switch (choice)
            {
                case "⚡ Run Postgres Backup (postgres_backup)":
                    await _backupWorkflowComponent.RunAsync(_settings);
                    break;
                case "⚙️ Configure Backup Settings":
                    _settings = _configureSettingsComponent.Configure(_settings);
                    break;
                case "🔌 Test PostgreSQL Connection":
                    await _testConnectionComponent.RunAsync(_settings);
                    break;
                case "🚪 Exit":
                    exit = true;
                    break;
            }
        }

        _mainMenuComponent.RenderGoodbye();
    }
}
