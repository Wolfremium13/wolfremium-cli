using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;
using Shouldly;
using Testcontainers.PostgreSql;
using Wolfremium.Cli.Database.Backup.Execute.Application.Contracts;
using Wolfremium.Cli.Database.Backup.Execute.Application.UseCases;
using Wolfremium.Cli.Database.Backup.Execute.Infrastructure.Postgres;
using Wolfremium.Cli.Database.Checker.Check.Infrastructure.Postgres;
using Wolfremium.Cli.Database.Shared.Infrastructure;
using Wolfremium.Cli.Tests.Database.Backup.Execute.Builders;
using Xunit;

namespace Wolfremium.Cli.Tests.Database.Backup.Execute.Infrastructure;

[Collection("ScenarioTests")]
public class BackupIntegrationTestsShould : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("test_db")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var connString = _postgresContainer.GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        await using (var cmd = new NpgsqlCommand("CREATE SCHEMA IF NOT EXISTS test_schema;", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new NpgsqlCommand(
            "CREATE TABLE IF NOT EXISTS public.users (" +
            "  id SERIAL PRIMARY KEY," +
            "  name VARCHAR(255) NOT NULL," +
            "  created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP" +
            ");", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new NpgsqlCommand(
            "INSERT INTO public.users (name) VALUES ('Alice'), ('Bob');", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
    }

    [Fact]
    public async Task GenerateSqlBackupFileWithExpectedContent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_test_backup.sql");
        
        var request = new BackupRequestBuilder()
            .WithHost(_postgresContainer.Hostname)
            .WithPort(_postgresContainer.GetMappedPublicPort(5432))
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithDatabase("test_db")
            .WithExportPath(tempFile)
            .Build();

        var databaseService = new PostgresDatabaseService();
        var backupWriter = new FileBackupWriter();
        var connectionCheckerService = new PostgresConnectionCheckerService();
        var dateTimeProvider = new SystemDateTimeProvider();
        var executeBackupUseCase = new ExecuteBackupUseCase(databaseService, backupWriter, connectionCheckerService, dateTimeProvider);
        var progressIndicator = new TestBackupProgressIndicator();

        try
        {
            var backupCommand = new BackupCommand(request, progressIndicator);
            var eitherResult = await executeBackupUseCase.ExecuteAsync(backupCommand);

            var successResult = eitherResult.Match(
                valid => valid,
                error => throw new Xunit.Sdk.XunitException($"Backup failed with error: {error.Message}")
            );
            
            successResult.Success.ShouldBeTrue();
            File.Exists(tempFile).ShouldBeTrue("Backup file was not created.");

            var sqlContent = await File.ReadAllTextAsync(tempFile);
            
            sqlContent.ShouldContain("-- POSTGRES SERVER CONFIGURATION");
            sqlContent.ShouldContain("-- ROLES CONFIGURATION");
            sqlContent.ShouldContain("-- DATABASE CONFIGURATION");
            sqlContent.ShouldContain("-- SCHEMAS CONFIGURATION");

            sqlContent.ShouldContain("CREATE TABLE \"users\"");
            sqlContent.ShouldContain("DROP TABLE IF EXISTS \"users\" CASCADE;");
            
            sqlContent.ShouldContain("INSERT INTO \"users\" (\"id\", \"name\", \"created_at\") VALUES (1, 'Alice'");
            sqlContent.ShouldContain("INSERT INTO \"users\" (\"id\", \"name\", \"created_at\") VALUES (2, 'Bob'");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private class TestBackupProgressIndicator : IBackupProgressIndicator
    {
        public void ReportProgress(string statusMessage, long bytesExported, double progressPercentage)
        {
        }
    }
}
