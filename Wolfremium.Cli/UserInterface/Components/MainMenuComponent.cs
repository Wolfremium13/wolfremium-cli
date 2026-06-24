using System.Threading;
using Spectre.Console;

namespace Wolfremium.Cli.UserInterface.Components;

public class MainMenuComponent
{
    public string ShowMenu()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[#88888e]Select an option from the menu:[/]")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up/down to reveal more options)[/]")
                .AddChoices(new[] {
                    "⚡ Run Postgres Backup (postgres_backup)",
                    "⚙️ Configure Backup Settings",
                    "🔌 Test PostgreSQL Connection",
                    "🚪 Exit"
                })
                .UseConverter(choice => choice)
        );
    }

    public void RenderGoodbye()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[#21d789]█   █   █  ▄▀▀▄  █     ██▀▀▀ █▀▀▀▄ ██▀▀▀ █▄  ▄█ ███ █    █ █▄  ▄█[/]");
        AnsiConsole.MarkupLine("[#21d789]█   █   █ █    █ █     ██▀▀  ██▀▀▀ ██▀▀  █ ▀▀ █  █  █    █ █ ▀▀ █[/]");
        AnsiConsole.MarkupLine("[#21d789] █▄█ █▄█   ▀▄▄▀  ██▄▄█ █     █  ▀▄ ██▄▄▄ █    █ ███  ▀▄▄▀  █    █[/]");
        AnsiConsole.MarkupLine("[bold #21d789]Goodbye, Wolfremium! Developer mode deactivated.[/]");
        Thread.Sleep(1000);
    }
}
