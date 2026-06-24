using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Backup.Execute.Application.Contracts;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Models;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Ports;
using Wolfremium.Cli.Database.Checker.Check.Domain.Models;
using Wolfremium.Cli.Database.Checker.Check.Domain.Ports;
using Wolfremium.Cli.Database.Shared.Domain.Ports;

namespace Wolfremium.Cli.Database.Backup.Execute.Application.UseCases;

public class ExecuteBackupUseCase : IExecuteBackup
{
    private readonly IDatabaseService _databaseService;
    private readonly IBackupWriter _backupWriter;
    private readonly IConnectionCheckerService _connectionCheckerService;
    private readonly IDateTimeProvider _dateTimeProvider;

    private const double InitialConnectionProgressPercentage = 2.0;
    private const double RolesProgressPercentage = 4.0;
    private const double SchemasProgressPercentage = 6.0;
    private const double TablesStartProgressPercentage = 10.0;
    private const double SavingFileProgressPercentage = 95.0;
    private const double CompletedProgressPercentage = 100.0;
    private const double TableDumpingWeightProgressPercentage = 80.0;
    private const double TableSchemaWeightProgressPercentage = 0.2;
    private const double TableDataWeightProgressPercentage = 1.0;

    public ExecuteBackupUseCase(
        IDatabaseService databaseService, 
        IBackupWriter backupWriter, 
        IConnectionCheckerService connectionCheckerService,
        IDateTimeProvider dateTimeProvider)
    {
        _databaseService = databaseService;
        _backupWriter = backupWriter;
        _connectionCheckerService = connectionCheckerService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Either<Error, BackupResult>> ExecuteAsync(BackupCommand command)
    {
        var settingsEither = BackupSettings.Create(
            command.Request.Host,
            command.Request.Port,
            command.Request.Username,
            command.Request.Password,
            command.Request.Database,
            command.Request.ExportPath,
            _dateTimeProvider
        );

        var resultAsync = 
            from settings in settingsEither.ToAsync()
            from connectionSettings in ConnectionSettings.Create(settings.Host, settings.Port, settings.Username, settings.Password, settings.Database).ToAsync()
            from connection in _connectionCheckerService.CheckConnectionAsync(connectionSettings).ToAsync()
            from progressReportConnected in ReportProgress(command.ProgressIndicator, "Connected. Fetching system configurations...", 0, InitialConnectionProgressPercentage)
            
            from initialization in _backupWriter.InitializeAsync(settings.ExportPath).ToAsync()
            from header in WriteHeaderInfoAsync(settings).ToAsync()
            from progressReportRoles in ReportProgress(command.ProgressIndicator, "Fetching role configurations...", 0, RolesProgressPercentage)
            
            from roles in DumpRolesAsync(settings).ToAsync()
            from progressReportSchemas in ReportProgress(command.ProgressIndicator, "Fetching schema configurations...", 0, SchemasProgressPercentage)
            
            from schemas in DumpSchemasAsync(settings).ToAsync()
            from progressReportTables in ReportProgress(command.ProgressIndicator, "Connected. Fetching tables from database...", 0, TablesStartProgressPercentage)
            
            from totalBytes in DumpTablesAsync(settings, command.ProgressIndicator).ToAsync()
            from progressReportSaving in ReportProgress(command.ProgressIndicator, "Saving backup SQL file to disk...", totalBytes, SavingFileProgressPercentage)
            
            from dispose in DisposeWriterAsync().ToAsync()
            from progressReportComplete in ReportProgress(command.ProgressIndicator, "Backup complete.", totalBytes, CompletedProgressPercentage)
            select new BackupResult(true, $"Backup completed successfully. Exported to: {settings.ExportPath}");

        return await resultAsync.ToEither();
    }

    private EitherAsync<Error, Unit> ReportProgress(IBackupProgressIndicator progressIndicator, string message, long bytes, double percentage)
    {
        progressIndicator.ReportProgress(message, bytes, percentage);
        return EitherAsync<Error, Unit>.Right(Unit.Default);
    }

    private async Task<Either<Error, Unit>> DisposeWriterAsync()
    {
        try
        {
            await _backupWriter.DisposeAsync();
            return Unit.Default;
        }
        catch (Exception exception)
        {
            return Error.New(new BackupWriterException("Failed to dispose backup writer.", exception));
        }
    }

    private async Task<Either<Error, Unit>> WriteHeaderInfoAsync(BackupSettings settings)
    {
        var writeResult = await _backupWriter.WriteLineAsync($"-- Wolfremium CLI Postgres Backup");
        if (writeResult.IsLeft) return writeResult;
        
        await _backupWriter.WriteLineAsync($"-- Generated at: {_dateTimeProvider.Now:yyyy-MM-dd HH:mm:ss}");
        await _backupWriter.WriteLineAsync($"-- Database: {settings.Database}");
        await _backupWriter.WriteLineAsync();

        var settingsResult = await _databaseService.GetServerSettingsAsync(settings);
        return await settingsResult.MatchAsync(
            async postgresSettings =>
            {
                await _backupWriter.WriteLineAsync("-- ====================================================");
                await _backupWriter.WriteLineAsync("-- POSTGRES SERVER CONFIGURATION");
                await _backupWriter.WriteLineAsync("-- ====================================================");
                foreach (var setting in postgresSettings)
                {
                    await _backupWriter.WriteLineAsync(setting);
                }
                await _backupWriter.WriteLineAsync();
                return Unit.Default;
            },
            async error =>
            {
                await _backupWriter.WriteLineAsync($"-- Warning: Could not dump server configuration settings. {error.Message}");
                await _backupWriter.WriteLineAsync();
                return Unit.Default;
            }
        );
    }

    private async Task<Either<Error, Unit>> DumpRolesAsync(BackupSettings settings)
    {
        var rolesResult = await _databaseService.GetRolesAsync(settings);
        
        return await rolesResult.MatchAsync(
            async roles => await WriteRolesListAsync(roles),
            async error => {
                await _backupWriter.WriteLineAsync($"-- Warning: Could not dump roles. {error.Message}");
                return await _backupWriter.WriteLineAsync();
            }
        );
    }

    private async Task<Either<Error, Unit>> WriteRolesListAsync(IReadOnlyList<RoleInformation> roles)
    {
        var writeHeadersAsync =
            from w1 in _backupWriter.WriteLineAsync("-- ====================================================").ToAsync()
            from w2 in _backupWriter.WriteLineAsync("-- ROLES CONFIGURATION").ToAsync()
            from w3 in _backupWriter.WriteLineAsync("-- ====================================================").ToAsync()
            select Unit.Default;

        var headersResult = await writeHeadersAsync.ToEither();
        if (headersResult.IsLeft) return headersResult;

        foreach (var role in roles)
        {
            var options = new List<string>();
            options.Add(role.IsSuperUser ? "SUPERUSER" : "NOSUPERUSER");
            options.Add(role.InheritsPrivileges ? "INHERIT" : "NOINHERIT");
            options.Add(role.CanCreateRole ? "CREATEROLE" : "NOCREATEROLE");
            options.Add(role.CanCreateDatabase ? "CREATEDB" : "NOCREATEDB");
            options.Add(role.CanLogin ? "LOGIN" : "NOLOGIN");

            var writeRoleAsync =
                from wr1 in _backupWriter.WriteLineAsync("DO $$").ToAsync()
                from wr2 in _backupWriter.WriteLineAsync("BEGIN").ToAsync()
                from wr3 in _backupWriter.WriteLineAsync($"  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '{role.RoleName}') THEN").ToAsync()
                from wr4 in _backupWriter.WriteLineAsync($"    CREATE ROLE \"{role.RoleName}\" WITH {string.Join(" ", options)};").ToAsync()
                from wr5 in _backupWriter.WriteLineAsync("  END IF;").ToAsync()
                from wr6 in _backupWriter.WriteLineAsync("END").ToAsync()
                from wr7 in _backupWriter.WriteLineAsync("$$;").ToAsync()
                select Unit.Default;

            var writeRoleResult = await writeRoleAsync.ToEither();
            if (writeRoleResult.IsLeft) return writeRoleResult;
        }

        return await _backupWriter.WriteLineAsync();
    }

    private async Task<Either<Error, Unit>> DumpSchemasAsync(BackupSettings settings)
    {
        var writeDbConfigAsync =
            from w1 in _backupWriter.WriteLineAsync("-- ====================================================").ToAsync()
            from w2 in _backupWriter.WriteLineAsync("-- DATABASE CONFIGURATION").ToAsync()
            from w3 in _backupWriter.WriteLineAsync("-- ====================================================").ToAsync()
            from w4 in _backupWriter.WriteLineAsync($"-- CREATE DATABASE \"{settings.Database}\";").ToAsync()
            from w5 in _backupWriter.WriteLineAsync().ToAsync()
            select Unit.Default;

        var dbConfigResult = await writeDbConfigAsync.ToEither();
        if (dbConfigResult.IsLeft) return dbConfigResult;

        var schemasResult = await _databaseService.GetSchemasAsync(settings);
        return await schemasResult.MatchAsync(
            async schemas => await WriteSchemasListAsync(schemas),
            async error => {
                await _backupWriter.WriteLineAsync($"-- Warning: Could not dump schemas. {error.Message}");
                return await _backupWriter.WriteLineAsync();
            }
        );
    }

    private async Task<Either<Error, Unit>> WriteSchemasListAsync(IReadOnlyList<string> schemas)
    {
        var writeHeadersAsync =
            from w1 in _backupWriter.WriteLineAsync("-- ====================================================").ToAsync()
            from w2 in _backupWriter.WriteLineAsync("-- SCHEMAS CONFIGURATION").ToAsync()
            from w3 in _backupWriter.WriteLineAsync("-- ====================================================").ToAsync()
            select Unit.Default;

        var headersResult = await writeHeadersAsync.ToEither();
        if (headersResult.IsLeft) return headersResult;

        foreach (var schema in schemas)
        {
            var res = await _backupWriter.WriteLineAsync($"CREATE SCHEMA IF NOT EXISTS \"{schema}\";");
            if (res.IsLeft) return res;
        }

        return await _backupWriter.WriteLineAsync();
    }

    private async Task<Either<Error, long>> DumpTablesAsync(BackupSettings settings, IBackupProgressIndicator progressIndicator)
    {
        var tablesQuery = 
            from tables in _databaseService.GetTablesAsync(settings).ToAsync()
            from totalBytes in DumpTablesListAsync(settings, tables, progressIndicator).ToAsync()
            select totalBytes;

        return await tablesQuery.ToEither();
    }

    private async Task<Either<Error, long>> DumpTablesListAsync(BackupSettings settings, IReadOnlyList<string> tables, IBackupProgressIndicator progressIndicator)
    {
        if (tables.Count == 0)
        {
            progressIndicator.ReportProgress("No tables found in public schema.", 0, TablesStartProgressPercentage);
            return 0L;
        }

        progressIndicator.ReportProgress($"Found {tables.Count} tables. Starting schema & data dump...", 0, TablesStartProgressPercentage);

        long totalBytes = 0;
        for (var tableIndex = 0; tableIndex < tables.Count; tableIndex++)
        {
            var table = tables[tableIndex];
            double progressStart = TablesStartProgressPercentage + (tableIndex * TableDumpingWeightProgressPercentage / tables.Count);
            double progressSchema = TablesStartProgressPercentage + ((tableIndex + TableSchemaWeightProgressPercentage) * TableDumpingWeightProgressPercentage / tables.Count);
            double progressData = TablesStartProgressPercentage + ((tableIndex + TableDataWeightProgressPercentage) * TableDumpingWeightProgressPercentage / tables.Count);

            progressIndicator.ReportProgress($"Dumping schema for table '{table}' ({tableIndex + 1}/{tables.Count})...", totalBytes, progressStart);

            var tableDumpAsync =
                from columns in _databaseService.GetTableColumnsAsync(settings, table).ToAsync()
                from schemaWrite in WriteSchemaAsync(table, columns).ToAsync()
                from data in _databaseService.GetTableDataAsync(settings, table).ToAsync()
                from bytes in WriteRowsAsync(table, columns, data, progressIndicator, progressSchema, totalBytes, progressData).ToAsync()
                select bytes;

            var tableDumpResult = await tableDumpAsync.ToEither();
            if (tableDumpResult.IsLeft)
            {
                var leftError = tableDumpResult.Match(right => default!, left => left);
                return Either<Error, long>.Left(leftError);
            }

            totalBytes += tableDumpResult.Match(right => right, _ => 0L);
            progressIndicator.ReportProgress($"Table '{table}' finished.", totalBytes, progressData);
        }

        return totalBytes;
    }

    private async Task<Either<Error, Unit>> WriteSchemaAsync(string table, IReadOnlyList<ColumnMetadata> columns)
    {
        var writeAsync =
            from writeComment in _backupWriter.WriteLineAsync($"-- Table Structure: {table}").ToAsync()
            from writeDrop in _backupWriter.WriteLineAsync($"DROP TABLE IF EXISTS \"{table}\" CASCADE;").ToAsync()
            from writeCreate in _backupWriter.WriteLineAsync($"CREATE TABLE \"{table}\" (").ToAsync()
            from writeColumnsResult in WriteColumnsAsync(columns).ToAsync()
            from writeClose in _backupWriter.WriteLineAsync(");").ToAsync()
            from writeEmptyLine in _backupWriter.WriteLineAsync().ToAsync()
            select Unit.Default;

        return await writeAsync.ToEither();
    }

    private async Task<Either<Error, Unit>> WriteColumnsAsync(IReadOnlyList<ColumnMetadata> columns)
    {
        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            var column = columns[columnIndex];
            var columnDefinition = $"  \"{column.Name}\" {MapDataType(column.Type)}";
            if (column.IsNullable == "NO") columnDefinition += " NOT NULL";
            if (column.DefaultValue != null) columnDefinition += $" DEFAULT {column.DefaultValue}";

            if (columnIndex < columns.Count - 1) columnDefinition += ",";
            var writeResult = await _backupWriter.WriteLineAsync(columnDefinition);
            if (writeResult.IsLeft)
            {
                var leftError = writeResult.Match(right => default!, left => left);
                return Either<Error, Unit>.Left(leftError);
            }
        }
        return Unit.Default;
    }

    private async Task<Either<Error, long>> WriteRowsAsync(
        string table, 
        IReadOnlyList<ColumnMetadata> columns, 
        IAsyncEnumerable<Dictionary<string, object>> data,
        IBackupProgressIndicator progressIndicator,
        double progressSchema,
        long currentTotalBytes,
        double progressData)
    {
        long localBytes = 0;
        progressIndicator.ReportProgress($"Dumping rows for table '{table}'...", currentTotalBytes, progressSchema);

        await foreach (var row in data)
        {
            var columnsList = new List<string>();
            var valuesList = new List<string>();
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                columnsList.Add($"\"{columns[columnIndex].Name}\"");
                valuesList.Add(FormatSqlValue(row[columns[columnIndex].Name]));
            }

            var insert = $"INSERT INTO \"{table}\" ({string.Join(", ", columnsList)}) VALUES ({string.Join(", ", valuesList)});";
            var writeResult = await _backupWriter.WriteLineAsync(insert);
            if (writeResult.IsLeft)
            {
                var leftError = writeResult.Match(right => default!, left => left);
                return Either<Error, long>.Left(leftError);
            }

            localBytes += Encoding.UTF8.GetByteCount(insert);
        }

        var emptyLineResult = await _backupWriter.WriteLineAsync();
        if (emptyLineResult.IsLeft)
        {
            var leftError = emptyLineResult.Match(right => default!, left => left);
            return Either<Error, long>.Left(leftError);
        }

        return localBytes;
    }

    private static string MapDataType(string postgresType)
    {
        return postgresType.ToUpper() switch
        {
            "CHARACTER VARYING" => "VARCHAR(255)",
            "INTEGER" => "INT",
            "BIGINT" => "BIGINT",
            "TIMESTAMP WITHOUT TIME ZONE" => "TIMESTAMP",
            "TIMESTAMP WITH TIME ZONE" => "TIMESTAMPTZ",
            _ => postgresType
        };
    }

    private static string FormatSqlValue(object val)
    {
        if (val == null || val == DBNull.Value) return "NULL";

        if (val is string s)
        {
            return $"'{s.Replace("'", "''")}'";
        }
        if (val is DateTime dt)
        {
            return $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'";
        }
        if (val is bool b)
        {
            return b ? "TRUE" : "FALSE";
        }
        if (val is double || val is float || val is decimal || val is int || val is long || val is short)
        {
            return val.ToString()!;
        }

        return $"'{val.ToString()!.Replace("'", "''")}'";
    }
}
