using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Npgsql;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Models;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Ports;

namespace Wolfremium.Cli.Database.Backup.Execute.Infrastructure.Postgres;

public class PostgresDatabaseService : IDatabaseService
{
    private const int ConnectionTimeoutSeconds = 5;
    private const int CommandTimeoutSeconds = 5;

    private string GetConnectionString(BackupSettings settings)
    {
        var passwordPart = string.IsNullOrWhiteSpace(settings.Password) ? "" : $"Password={settings.Password};";
        return $"Host={settings.Host};Port={settings.Port};Username={settings.Username};{passwordPart}Database={settings.Database};Timeout={ConnectionTimeoutSeconds};CommandTimeout={CommandTimeoutSeconds};";
    }

    public async Task<Either<Error, long>> GetDatabaseSizeAsync(BackupSettings settings)
    {
        try
        {
            var connectionString = GetConnectionString(settings);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"SELECT pg_database_size('{settings.Database}');", connection);
            var size = await command.ExecuteScalarAsync();
            return size is long sizeInBytes ? sizeInBytes : 0L;
        }
        catch (Exception exception)
        {
            return Error.New(new DatabaseSizeException(exception.Message, exception));
        }
    }

    public async Task<Either<Error, IReadOnlyList<string>>> GetServerSettingsAsync(BackupSettings settings)
    {
        try
        {
            var list = new List<string>();
            var connectionString = GetConnectionString(settings);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("SELECT name, setting, category, short_desc FROM pg_settings WHERE source = 'configuration file';", connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var setting = reader.IsDBNull(1) ? "NULL" : reader.GetString(1);
                var category = reader.IsDBNull(2) ? "General" : reader.GetString(2);
                var description = reader.IsDBNull(3) ? "" : reader.GetString(3);
                list.Add($"-- {name} = '{setting}'  # {category}: {description}");
            }
            return list;
        }
        catch (Exception exception)
        {
            return Error.New(new DatabaseQueryException("Failed to fetch server settings", exception));
        }
    }

    public async Task<Either<Error, IReadOnlyList<RoleInformation>>> GetRolesAsync(BackupSettings settings)
    {
        try
        {
            var list = new List<RoleInformation>();
            var connectionString = GetConnectionString(settings);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("SELECT rolname, rolsuper, rolinherit, rolcreaterole, rolcreatedb, rolcanlogin FROM pg_roles WHERE rolname NOT LIKE 'pg_%';", connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new RoleInformation(
                    reader.GetString(0),
                    reader.GetBoolean(1),
                    reader.GetBoolean(2),
                    reader.GetBoolean(3),
                    reader.GetBoolean(4),
                    reader.GetBoolean(5)
                ));
            }
            return list;
        }
        catch (Exception exception)
        {
            return Error.New(new DatabaseQueryException("Failed to fetch roles", exception));
        }
    }

    public async Task<Either<Error, IReadOnlyList<string>>> GetSchemasAsync(BackupSettings settings)
    {
        try
        {
            var list = new List<string>();
            var connectionString = GetConnectionString(settings);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT IN ('information_schema', 'pg_catalog', 'pg_toast');", connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(reader.GetString(0));
            }
            return list;
        }
        catch (Exception exception)
        {
            return Error.New(new DatabaseQueryException("Failed to fetch schemas", exception));
        }
    }

    public async Task<Either<Error, IReadOnlyList<string>>> GetTablesAsync(BackupSettings settings)
    {
        try
        {
            var list = new List<string>();
            var connectionString = GetConnectionString(settings);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name;", connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(reader.GetString(0));
            }
            return list;
        }
        catch (Exception exception)
        {
            return Error.New(new DatabaseQueryException("Failed to fetch tables", exception));
        }
    }

    public async Task<Either<Error, IReadOnlyList<ColumnMetadata>>> GetTableColumnsAsync(BackupSettings settings, string tableName)
    {
        try
        {
            var list = new List<ColumnMetadata>();
            var connectionString = GetConnectionString(settings);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                "SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns " +
                "WHERE table_schema = 'public' AND table_name = @table ORDER BY ordinal_position;", connection);
            command.Parameters.AddWithValue("table", tableName);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ColumnMetadata(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3)
                ));
            }
            return list;
        }
        catch (Exception exception)
        {
            return Error.New(new DatabaseQueryException($"Failed to fetch columns for table {tableName}", exception));
        }
    }

    public async Task<Either<Error, IAsyncEnumerable<Dictionary<string, object>>>> GetTableDataAsync(BackupSettings settings, string tableName)
    {
        try
        {
            var connectionString = GetConnectionString(settings);
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            async IAsyncEnumerable<Dictionary<string, object>> StreamData()
            {
                await using (connection)
                {
                    await using var command = new NpgsqlCommand($"SELECT * FROM \"{tableName}\";", connection);
                    await using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int index = 0; index < reader.FieldCount; index++)
                        {
                            row[reader.GetName(index)] = reader.GetValue(index);
                        }
                        yield return row;
                    }
                }
            }

            return Either<Error, IAsyncEnumerable<Dictionary<string, object>>>.Right(StreamData());
        }
        catch (Exception exception)
        {
            return Error.New(new DatabaseQueryException($"Failed to query rows from table {tableName}", exception));
        }
    }
}
