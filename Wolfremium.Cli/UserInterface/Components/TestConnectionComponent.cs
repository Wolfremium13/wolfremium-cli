using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Spectre.Console;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Models;
using Wolfremium.Cli.Database.Checker.Check.Application.Contracts;
using Wolfremium.Cli.Database.Checker.Check.Domain.Models;

namespace Wolfremium.Cli.UserInterface.Components;

public class TestConnectionComponent
{
    private readonly HeaderComponent _headerComponent;
    private readonly ICheckConnection _checkConnection;

    public TestConnectionComponent(HeaderComponent headerComponent, ICheckConnection checkConnection)
    {
        _headerComponent = headerComponent;
        _checkConnection = checkConnection;
    }

    public async Task RunAsync(BackupSettings config)
    {
        AnsiConsole.Clear();
        _headerComponent.Render();

        AnsiConsole.MarkupLine("[bold #21d789]Testing connection to database...[/]");
        AnsiConsole.WriteLine();

        (bool Success, string Message) result = (false, "");

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("#21d789"))
            .StartAsync("Connecting...", async ctx =>
            {
                var settingsEither = ConnectionSettings.Create(config.Host, config.Port, config.Username, config.Password, config.Database);
                var eitherResult = await settingsEither.MatchAsync(
                    async settings => await _checkConnection.ExecuteAsync(new ConnectionCheckRequest(settings)),
                    error => Task.FromResult<Either<Error, string>>(error)
                );
                
                var mappedResult = from version in eitherResult
                                   select (Success: true, Message: version);

                result = mappedResult.IfLeft(error => (Success: false, Message: error.Message));
            });

        if (result.Success)
        {
            AnsiConsole.MarkupLine("[bold #21d789]✔ CONNECTION SUCCESSFUL![/]");
            AnsiConsole.MarkupLine($"[grey]Postgres Version:[/] {result.Message}");
        }
        else
        {
            AnsiConsole.MarkupLine("[bold red]✖ CONNECTION FAILED![/]");
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(result.Message)}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to return to menu...[/]");
        Console.ReadKey(true);
    }
}
