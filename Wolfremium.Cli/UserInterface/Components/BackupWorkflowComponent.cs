using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console;
using Wolfremium.Cli.Database.Backup.Execute.Application.Contracts;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Models;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Ports;
using Wolfremium.Cli.Database.Checker.Check.Domain.Models;
using Wolfremium.Cli.Database.Checker.Check.Domain.Ports;

namespace Wolfremium.Cli.UserInterface.Components;

public class BackupWorkflowComponent
{
    private readonly HeaderComponent _headerComponent;
    private readonly IExecuteBackup _executeBackup;
    private readonly IDatabaseService _databaseService;
    private readonly IConnectionCheckerService _connectionCheckerService;

    public BackupWorkflowComponent(
        HeaderComponent headerComponent, 
        IExecuteBackup executeBackup,
        IDatabaseService databaseService,
        IConnectionCheckerService connectionCheckerService)
    {
        _headerComponent = headerComponent;
        _executeBackup = executeBackup;
        _databaseService = databaseService;
        _connectionCheckerService = connectionCheckerService;
    }

    public async Task RunAsync(BackupSettings config)
    {
        AnsiConsole.Clear();
        _headerComponent.Render();

        AnsiConsole.MarkupLine("[bold #21d789]Starting postgres_backup Workflow (Pure C# Engine)...[/]");
        AnsiConsole.WriteLine();

        long dbSize = 0;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("#21d789"))
            .StartAsync("Testing connection and retrieving database statistics...", async ctx =>
            {
                var settingsEither = ConnectionSettings.Create(config.Host, config.Port, config.Username, config.Password, config.Database);
                var sizeQuery = 
                    from settings in settingsEither.ToAsync()
                    from connection in _connectionCheckerService.CheckConnectionAsync(settings).ToAsync()
                    from size in _databaseService.GetDatabaseSizeAsync(config).ToAsync()
                    select size;

                var sizeResult = await sizeQuery.ToEither();
                dbSize = sizeResult.IfLeft(0L);
            });

        var logList = new List<string>();
        var startTime = DateTime.Now;
        var statusMessage = "Initializing backup streams...";

        var request = new BackupRequest(
            config.Host,
            config.Port,
            config.Username,
            config.Password,
            config.Database,
            config.ExportPath
        );

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

                    var table = new Table().BorderColor(Color.FromHex("#2d2d30")).Border(TableBorder.Rounded);
                    table.AddColumn("[#21d789]Metric[/]");
                    table.AddColumn("[#7f52ff]Details[/]");

                    table.AddRow("Status", $"[yellow]{Markup.Escape(statusMessage)}[/]");
                    table.AddRow("Database", $"{config.Database} @ {config.Host}:{config.Port}");
                    table.AddRow("Export Path", config.ExportPath);
                    table.AddRow("Engine Mode", "Pure C# SQL Parser (No local pg_dump dependency)");
                    
                    var bytesStr = (currentBytes / 1024.0) >= 1024.0 
                         ? $"[bold]{(currentBytes / (1024.0 * 1024.0)):N2} MB[/]" 
                         : $"[bold]{(currentBytes / 1024.0):N2} KB[/]";
                    table.AddRow("Bytes Exported", bytesStr);
                    table.AddRow("Elapsed Time", $"{(DateTime.Now - startTime).ToString(@"mm\:ss")}");

                    string progressBar;
                    var percentage = (int)Math.Min(100, progress);
                    var barWidth = 30;
                    var filled = (int)(percentage / 100.0 * barWidth);
                    var dbSizeStr = dbSize > 0 ? $" (DB Size: {(dbSize / (1024.0 * 1024.0)):N2} MB)" : "";
                    var rawBar = $"[{new string('█', filled)}{new string('░', barWidth - filled)}] {percentage}%{dbSizeStr}";
                    
                    progressBar = Markup.Escape(rawBar);
                    table.AddRow("Progress Reference", progressBar);

                    var logsText = string.Join("\n", logList);
                    var logPanel = new Panel(logsText)
                        .Header("[#88888e]Latest Backup Progress Logs[/]")
                        .BorderColor(Color.FromHex("#2d2d30"))
                        .Expand();

                    var spinnerFrames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
                    var currentFrame = spinnerFrames[(int)((DateTime.Now.Ticks / 1000000) % spinnerFrames.Length)];

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

                var progressIndicator = new LiveProgressIndicator(RenderDashboard);
                var backupCommand = new BackupCommand(request, progressIndicator);

                var backupEither = await _executeBackup.ExecuteAsync(backupCommand);

                AnsiConsole.WriteLine();
                backupEither.Match(
                    backupResult =>
                    {
                        var fileUri = $"file:///{Path.GetFullPath(config.ExportPath).Replace('\\', '/')}";
                        var successMessage = $"[bold #21d789]✔ SUCCESS[/]\n\n" +
                                             $"{backupResult.Message}\n\n" +
                                             $"[bold #21d789]Link to Backup:[/] [link={fileUri}]{Markup.Escape(config.ExportPath)}[/]";

                        var successPanel = new Panel(new Markup(successMessage))
                            .BorderColor(Color.FromHex("#21d789"))
                            .Header("[bold #21d789]Backup Session Completed[/]")
                            .Padding(1, 1, 1, 1);
                        AnsiConsole.Write(successPanel);
                    },
                    error =>
                    {
                        string customMessage = error.Exception.Match(
                            ex => ex switch {
                                Wolfremium.Cli.Database.Checker.Check.Domain.Exceptions.DatabaseConnectionException => $"[red]Database Connection failed:[/] {ex.Message}",
                                Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions.DatabaseConnectionException => $"[red]Database Connection failed:[/] {ex.Message}",
                                Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions.DatabaseSizeException => $"[red]Database Size retrieval failed:[/] {ex.Message}",
                                Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions.DatabaseQueryException => $"[red]Database Schema Query failed:[/] {ex.Message}",
                                Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions.BackupWriterException => $"[red]Backup File Writing failed:[/] {ex.Message}",
                                Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions.ValidationException => $"[red]Validation failed:[/] {ex.Message}",
                                Wolfremium.Cli.Database.Checker.Check.Domain.Exceptions.ValidationException => $"[red]Validation failed:[/] {ex.Message}",
                                _ => $"[red]Backup failed with error:[/] {ex.Message}"
                            },
                            () => $"[red]Backup failed with error:[/] {error.Message}"
                        );

                        var failPanel = new Panel(new Markup($"[bold red]✖ FAILED[/]\n\n{customMessage}"))
                            .BorderColor(Color.Red)
                            .Header("[bold red]Backup Session Error[/]")
                            .Padding(1, 1, 1, 1);
                        AnsiConsole.Write(failPanel);
                    }
                );
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to return to menu...[/]");
        Console.ReadKey(true);
    }

    private class LiveProgressIndicator : IBackupProgressIndicator
    {
        private readonly Action<string, long, double> _onProgress;

        public LiveProgressIndicator(Action<string, long, double> onProgress)
        {
            _onProgress = onProgress;
        }

        public void ReportProgress(string statusMessage, long bytesExported, double progressPercentage)
        {
            _onProgress(statusMessage, bytesExported, progressPercentage);
        }
    }
}
