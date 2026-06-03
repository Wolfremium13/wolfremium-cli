using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace Wolfremium.Cli;

public class Program
{
    private static BackupConfig _config = new();

    public static async Task Main(string[] args)
    {
        // Load initial settings from environment
        _config = BackupConfig.LoadFromEnv();

        Console.Title = "Wolfremium CLI - Developer Tools";

        var exit = false;
        while (!exit)
        {
            Console.Clear();
            RenderHeader();

            var choice = AnsiConsole.Prompt(
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

            switch (choice)
            {
                case "⚡ Run Postgres Backup (postgres_backup)":
                    await RunBackupWorkflowAsync();
                    break;
                case "⚙️ Configure Backup Settings":
                    await ConfigureSettingsWorkflowAsync();
                    break;
                case "🔌 Test PostgreSQL Connection":
                    await TestConnectionWorkflowAsync();
                    break;
                case "🚪 Exit":
                    exit = true;
                    break;
            }
        }

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[#21d789] █ █ █ █▀█ █   █▀  █▀▄ █▀▀ █▄ ▄█ █ █ █ █▄ ▄█[/]");
        AnsiConsole.MarkupLine("[#21d789] ▀▄▀▄▀ █▄█ █▄▄ █   █ █ █▄▄ █ ▀ █ █ █▄█ █ ▀ █[/]");
        AnsiConsole.MarkupLine("[bold #21d789]Goodbye, Wolfremium! Developer mode deactivated.[/]");
        Thread.Sleep(1000);
    }

    private static void RenderHeader()
    {
        // Custom hand-drawn thin ASCII banner for "wolfremium"
        AnsiConsole.MarkupLine("[#21d789] █ █ █ █▀█ █   █▀  █▀▄ █▀▀ █▄ ▄█ █ █ █ █▄ ▄█[/]");
        AnsiConsole.MarkupLine("[#21d789] ▀▄▀▄▀ █▄█ █▄▄ █   █ █ █▄▄ █ ▀ █ █ █▄█ █ ▀ █[/]");
        
        // Subheader rule with brand colors
        var rule = new Rule("[bold #ffffff]DEVELOPER CLI[/]");
        rule.Justification = Justify.Left;
        rule.Style = Style.Parse("#7f52ff");
        AnsiConsole.Write(rule);
        
        AnsiConsole.WriteLine();
    }

    private static async Task RunBackupWorkflowAsync()
    {
        Console.Clear();
        RenderHeader();

        AnsiConsole.MarkupLine("[bold #21d789]Starting postgres_backup Workflow (Pure C# Engine)...[/]");
        AnsiConsole.WriteLine();

        long dbSize = 0;

        // Connection test & size retrieval
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("#21d789"))
            .StartAsync("Testing connection and retrieving database statistics...", async ctx =>
            {
                var (success, _) = await ConnectionTester.TestConnectionAsync(_config);
                if (success)
                {
                    dbSize = await GetDatabaseSizeAsync(_config);
                }
            });

        var logList = new List<string>();
        var startTime = DateTime.Now;
        var statusMessage = "Initializing backup streams...";

        // Set up the Live Display Dashboard
        await AnsiConsole.Live(new Panel("Preparing backup view..."))
            .StartAsync(async ctx =>
            {
                 void RenderDashboard(string currentLogLine, long currentBytes, double progress)
                {
                    if (!string.IsNullOrWhiteSpace(currentLogLine))
                    {
                        statusMessage = currentLogLine;
                        lock (logList)
                        {
                            logList.Add(currentLogLine);
                            if (logList.Count > 5) logList.RemoveAt(0);
                        }
                    }

                    // Create metrics table
                    var table = new Table().BorderColor(Color.FromHex("#2d2d30")).Border(TableBorder.Rounded);
                    table.AddColumn("[#21d789]Metric[/]");
                    table.AddColumn("[#7f52ff]Details[/]");

                    table.AddRow("Status", $"[yellow]{Markup.Escape(statusMessage)}[/]");
                    table.AddRow("Database", $"{_config.Database} @ {_config.Host}:{_config.Port}");
                    table.AddRow("Export Path", _config.ExportPath);
                    table.AddRow("Engine Mode", "Pure C# SQL Parser (No local pg_dump dependency)");
                    
                    var bytesStr = (currentBytes / 1024.0) >= 1024.0 
                        ? $"[bold]{(currentBytes / (1024.0 * 1024.0)):N2} MB[/]" 
                        : $"[bold]{(currentBytes / 1024.0):N2} KB[/]";
                    table.AddRow("Bytes Exported", bytesStr);
                    table.AddRow("Elapsed Time", $"{(DateTime.Now - startTime).ToString(@"mm\:ss")}");

                    // Render progress bar
                    string progressBar;
                    var percentage = (int)Math.Min(100, progress);
                    var barWidth = 30;
                    var filled = (int)(percentage / 100.0 * barWidth);
                    var dbSizeStr = dbSize > 0 ? $" (DB Size: {(dbSize / (1024.0 * 1024.0)):N2} MB)" : "";
                    var rawBar = $"[{new string('█', filled)}{new string('░', barWidth - filled)}] {percentage}%{dbSizeStr}";
                    
                    // Escape brackets to prevent Spectre from interpreting them as style tags
                    progressBar = Markup.Escape(rawBar);
                    table.AddRow("Progress Reference", progressBar);

                    // Create logs panel
                    var logsText = string.Join("\n", logList);
                    var logPanel = new Panel(logsText)
                        .Header("[#88888e]Latest Backup Progress Logs[/]")
                        .BorderColor(Color.FromHex("#2d2d30"))
                        .Expand();

                    // Spinner frame animation
                    var spinnerFrames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
                    var currentFrame = spinnerFrames[(int)((DateTime.Now.Ticks / 1000000) % spinnerFrames.Length)];

                    // Grid container
                    var grid = new Grid();
                    grid.AddColumn();
                    grid.AddRow(new Rule("[#21d789]Wolfremium Active Database Backup[/]").RuleStyle("#7f52ff"));
                    grid.AddRow(table);
                    grid.AddRow(logPanel);
                    grid.AddRow(new Markup($"[#21d789]{currentFrame} Extracting schema & table rows...[/]"));

                    var outerPanel = new Panel(grid)
                        .BorderColor(Color.FromHex("#21d789"))
                        .Padding(1, 1, 1, 1);

                    ctx.UpdateTarget(outerPanel);
                }

                // Trigger execution
                var backupResult = await BackupExecutor.ExecuteBackupAsync(_config, (line, bytes, progress) =>
                {
                    RenderDashboard(line, bytes, progress);
                });

                // Final screen refresh
                AnsiConsole.WriteLine();
                if (backupResult.Success)
                {
                    var successPanel = new Panel(new Markup($"[bold #21d789]✔ SUCCESS[/]\n\n{backupResult.Message}"))
                        .BorderColor(Color.FromHex("#21d789"))
                        .Header("[bold #21d789]Backup Session Completed[/]")
                        .Padding(1, 1, 1, 1);
                    AnsiConsole.Write(successPanel);
                }
                else
                {
                    var failPanel = new Panel(new Markup($"[bold red]✖ FAILED[/]\n\n[red]{Markup.Escape(backupResult.Message)}[/]"))
                        .BorderColor(Color.Red)
                        .Header("[bold red]Backup Session Error[/]")
                        .Padding(1, 1, 1, 1);
                    AnsiConsole.Write(failPanel);
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to return to menu...[/]");
        Console.ReadKey(true);
    }

    private static async Task<long> GetDatabaseSizeAsync(BackupConfig config)
    {
        try
        {
            await using var conn = new Npgsql.NpgsqlConnection(config.GetConnectionString());
            await conn.OpenAsync();
            await using var cmd = new Npgsql.NpgsqlCommand($"SELECT pg_database_size('{config.Database}');", conn);
            var size = await cmd.ExecuteScalarAsync();
            return size is long l ? l : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static async Task TestConnectionWorkflowAsync()
    {
        Console.Clear();
        RenderHeader();

        AnsiConsole.MarkupLine("[bold #21d789]Testing connection to database...[/]");
        AnsiConsole.WriteLine();

        (bool Success, string Message) result = (false, "");

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("#21d789"))
            .StartAsync("Connecting...", async ctx =>
            {
                result = await ConnectionTester.TestConnectionAsync(_config);
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

    private static Task ConfigureSettingsWorkflowAsync()
    {
        var done = false;
        while (!done)
        {
            Console.Clear();
            RenderHeader();

            AnsiConsole.MarkupLine("[bold #21d789]⚙️ Session Configuration Settings[/]");
            AnsiConsole.MarkupLine("[grey]These settings live in memory for this CLI session (or loaded from Env variables).[/]");
            AnsiConsole.WriteLine();

            // Display current settings table
            var table = new Table().BorderColor(Color.FromHex("#2d2d30")).Border(TableBorder.Rounded);
            table.AddColumn("[#21d789]Setting Option[/]");
            table.AddColumn("[#7f52ff]Current Value[/]");

            table.AddRow("1. DB Host", _config.Host);
            table.AddRow("2. DB Port", _config.Port.ToString());
            table.AddRow("3. DB User", _config.Username);
            table.AddRow("4. DB Password", string.IsNullOrEmpty(_config.Password) ? "[grey](not set)[/]" : new string('*', _config.Password.Length));
            table.AddRow("5. DB Name", _config.Database);
            table.AddRow("6. Export Path", _config.ExportPath);

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
                    _config.Host = AnsiConsole.Ask<string>("Enter Hostname / IP:", _config.Host);
                    break;
                case "Port":
                    _config.Port = AnsiConsole.Ask<int>("Enter Port:", _config.Port);
                    break;
                case "User":
                    _config.Username = AnsiConsole.Ask<string>("Enter DB User:", _config.Username);
                    break;
                case "Password":
                    _config.Password = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter DB Password:")
                            .Secret()
                            .AllowEmpty()
                    );
                    break;
                case "Database Name":
                    _config.Database = AnsiConsole.Ask<string>("Enter DB Name:", _config.Database);
                    break;
                case "Export Path":
                    _config.ExportPath = AnsiConsole.Ask<string>("Enter Target Backup File Path:", _config.ExportPath);
                    break;
                case "<- Back to Main Menu":
                    done = true;
                    break;
            }
        }

        return Task.CompletedTask;
    }
}