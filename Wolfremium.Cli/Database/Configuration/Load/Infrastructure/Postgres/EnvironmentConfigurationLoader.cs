using System;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Models;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Ports;

namespace Wolfremium.Cli.Database.Configuration.Load.Infrastructure.Postgres;

public class EnvironmentConfigurationLoader : IConfigurationLoader
{
    private const int DefaultPostgresPort = 5432;

    public Task<Either<Error, ApplicationConfiguration>> LoadAsync()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = DefaultPostgresPort;
        if (Environment.GetEnvironmentVariable("DB_PORT") is { } portStr && int.TryParse(portStr, out var p)) port = p;
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var pass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "mysecretpassword";
        var db = Environment.GetEnvironmentVariable("DB_NAME") ?? "wolfremium_dev";
        var path = Environment.GetEnvironmentVariable("BACKUP_PATH");

        return Task.FromResult(ApplicationConfiguration.Create(host, port, user, pass, db, path));
    }
}
