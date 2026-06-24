using Spectre.Console;

namespace Wolfremium.Cli.UserInterface.Components;

public class HeaderComponent
{
    public void Render()
    {
        AnsiConsole.MarkupLine("[#21d789]█   █   █  ▄▀▀▄  █     ██▀▀▀ █▀▀▀▄ ██▀▀▀ █▄  ▄█ ███ █    █ █▄  ▄█[/]");
        AnsiConsole.MarkupLine("[#21d789]█   █   █ █    █ █     ██▀▀  ██▀▀▀ ██▀▀  █ ▀▀ █  █  █    █ █ ▀▀ █[/]");
        AnsiConsole.MarkupLine("[#21d789] █▄█ █▄█   ▀▄▄▀  ██▄▄█ █     █  ▀▄ ██▄▄▄ █    █ ███  ▀▄▄▀  █    █[/]");
        
        var rule = new Rule("[bold #ffffff]DEVELOPER CLI[/]");
        rule.Justification = Justify.Left;
        rule.Style = Style.Parse("#7f52ff");
        AnsiConsole.Write(rule);
        
        AnsiConsole.WriteLine();
    }
}
