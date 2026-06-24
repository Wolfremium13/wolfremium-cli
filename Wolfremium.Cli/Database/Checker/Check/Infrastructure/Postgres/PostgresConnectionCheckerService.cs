using System;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Npgsql;
using Wolfremium.Cli.Database.Checker.Check.Domain.Exceptions;
using Wolfremium.Cli.Database.Checker.Check.Domain.Models;
using Wolfremium.Cli.Database.Checker.Check.Domain.Ports;

namespace Wolfremium.Cli.Database.Checker.Check.Infrastructure.Postgres;

public class PostgresConnectionCheckerService : IConnectionCheckerService
{
    private const int ConnectionTimeoutSeconds = 5;
    private const int CommandTimeoutSeconds = 5;

    private string GetConnectionString(ConnectionSettings settings)
    {
        var passwordPart = string.IsNullOrWhiteSpace(settings.Password) ? "" : $"Password={settings.Password};";
        return $"Host={settings.Host};Port={settings.Port};Username={settings.Username};{passwordPart}Database={settings.Database};Timeout={ConnectionTimeoutSeconds};CommandTimeout={CommandTimeoutSeconds};";
    }

    public async Task<Either<Error, string>> CheckConnectionAsync(ConnectionSettings settings)
    {
        try
        {
            var connectionString = GetConnectionString(settings);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("SELECT version();", connection);
            var version = await command.ExecuteScalarAsync();

            return version?.ToString() ?? "Connected successfully.";
        }
        catch (Exception exception)
        {
            return Error.New(new DatabaseConnectionException(exception.Message, exception));
        }
    }
}
