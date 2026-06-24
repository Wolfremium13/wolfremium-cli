using Spectre.Console;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Models;
using Wolfremium.Cli.Database.Shared.Domain.Ports;

namespace Wolfremium.Cli.UserInterface.Components;

public class ConfigureSettingsComponent
{
    private readonly HeaderComponent _headerComponent;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConfigureSettingsComponent(HeaderComponent headerComponent, IDateTimeProvider dateTimeProvider)
    {
        _headerComponent = headerComponent;
        _dateTimeProvider = dateTimeProvider;
    }

    public BackupSettings Configure(BackupSettings config)
    {
        var current = config;
        var done = false;
        while (!done)
        {
            AnsiConsole.Clear();
            _headerComponent.Render();

            AnsiConsole.MarkupLine("[bold #21d789]⚙️ Session Configuration Settings[/]");
            AnsiConsole.MarkupLine("[grey]These settings live in memory for this CLI session (or loaded from Env variables).[/]");
            AnsiConsole.WriteLine();

            var table = new Table().BorderColor(Color.FromHex("#2d2d30")).Border(TableBorder.Rounded);
            table.AddColumn("[#21d789]Setting Option[/]");
            table.AddColumn("[#7f52ff]Current Value[/]");

            table.AddRow("1. DB Host", current.Host);
            table.AddRow("2. DB Port", current.Port.ToString());
            table.AddRow("3. DB User", current.Username);
            table.AddRow("4. DB Password", string.IsNullOrEmpty(current.Password) ? "[grey](not set)[/]" : new string('*', current.Password.Length));
            table.AddRow("5. DB Name", current.Database);
            table.AddRow("6. Export Path", current.ExportPath);

            AnsiConsole.Write(table);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[#88888e]Select a setting to edit or go back:[/]")
                    .AddChoices(new[] {
                        "Host", "Port", "User", "Password", "Database Name", 
                        "Export Path", "<- Back to Main Menu"
                    })
            );

            switch (choice)
            {
                case "Host":
                    var newHost = AnsiConsole.Ask<string>("Enter Hostname / IP:", current.Host);
                    BackupSettings.Create(newHost, current.Port, current.Username, current.Password, current.Database, current.ExportPath, _dateTimeProvider)
                        .Match(valid => current = valid, error => AnsiConsole.MarkupLine($"[red]Error: {error.Message}[/]"));
                    break;
                case "Port":
                    var newPort = AnsiConsole.Ask<int>("Enter Port:", current.Port);
                    BackupSettings.Create(current.Host, newPort, current.Username, current.Password, current.Database, current.ExportPath, _dateTimeProvider)
                        .Match(valid => current = valid, error => AnsiConsole.MarkupLine($"[red]Error: {error.Message}[/]"));
                    break;
                case "User":
                    var newUser = AnsiConsole.Ask<string>("Enter DB User:", current.Username);
                    BackupSettings.Create(current.Host, current.Port, newUser, current.Password, current.Database, current.ExportPath, _dateTimeProvider)
                        .Match(valid => current = valid, error => AnsiConsole.MarkupLine($"[red]Error: {error.Message}[/]"));
                    break;
                case "Password":
                    var newPass = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter DB Password:")
                            .Secret()
                            .AllowEmpty()
                    );
                    BackupSettings.Create(current.Host, current.Port, current.Username, newPass, current.Database, current.ExportPath, _dateTimeProvider)
                        .Match(valid => current = valid, error => AnsiConsole.MarkupLine($"[red]Error: {error.Message}[/]"));
                    break;
                case "Database Name":
                    var newDb = AnsiConsole.Ask<string>("Enter DB Name:", current.Database);
                    BackupSettings.Create(current.Host, current.Port, current.Username, current.Password, newDb, current.ExportPath, _dateTimeProvider)
                        .Match(valid => current = valid, error => AnsiConsole.MarkupLine($"[red]Error: {error.Message}[/]"));
                    break;
                case "Export Path":
                    var newPath = AnsiConsole.Ask<string>("Enter Target Backup File Path:", current.ExportPath);
                    BackupSettings.Create(current.Host, current.Port, current.Username, current.Password, current.Database, newPath, _dateTimeProvider)
                        .Match(valid => current = valid, error => AnsiConsole.MarkupLine($"[red]Error: {error.Message}[/]"));
                    break;
                case "<- Back to Main Menu":
                    done = true;
                    break;
            }
        }
        return current;
    }
}
