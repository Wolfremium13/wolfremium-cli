using System;
using System.Threading.Tasks;
using Shouldly;
using Testcontainers.PostgreSql;
using Wolfremium.Cli.Database.Checker.Check.Application.Contracts;
using Wolfremium.Cli.Database.Checker.Check.Application.UseCases;
using Wolfremium.Cli.Database.Checker.Check.Domain.Exceptions;
using Wolfremium.Cli.Database.Checker.Check.Domain.Models;
using Wolfremium.Cli.Database.Checker.Check.Infrastructure.Postgres;
using Xunit;

namespace Wolfremium.Cli.Tests.Database.Checker.Check.Infrastructure;

[Collection("ScenarioTests")]
public class ConnectionCheckerIntegrationTestsShould : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("test_check_db")
        .WithUsername("test_check_user")
        .WithPassword("test_check_password")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
    }

    [Fact]
    public async Task VerifyConnectionWithValidCredentialsSuccessfully()
    {
        var settingsEither = ConnectionSettings.Create(
            _postgresContainer.Hostname,
            _postgresContainer.GetMappedPublicPort(5432),
            "test_check_user",
            "test_check_password",
            "test_check_db"
        );

        var settings = settingsEither.Match(
            valid => valid,
            error => throw new Exception(error.Message)
        );

        var service = new PostgresConnectionCheckerService();
        var useCase = new CheckConnectionUseCase(service);
        var result = await useCase.ExecuteAsync(new ConnectionCheckRequest(settings));

        result.IsRight.ShouldBeTrue();
        result.Match(
            version => version.ShouldContain("PostgreSQL"),
            error => error.ShouldBeNull()
        );
    }

    [Fact]
    public async Task FailConnectionWhenPasswordIsInvalid()
    {
        var settingsEither = ConnectionSettings.Create(
            _postgresContainer.Hostname,
            _postgresContainer.GetMappedPublicPort(5432),
            "test_check_user",
            "wrong_password",
            "test_check_db"
        );

        var settings = settingsEither.Match(
            valid => valid,
            error => throw new Exception(error.Message)
        );

        var service = new PostgresConnectionCheckerService();
        var useCase = new CheckConnectionUseCase(service);
        var result = await useCase.ExecuteAsync(new ConnectionCheckRequest(settings));

        result.IsLeft.ShouldBeTrue();
        result.Match(
            _ => Assert.Fail("Should have failed"),
            error =>
            {
                var exception = error.Exception.Match<Exception>(ex => ex, () => throw new InvalidOperationException("No exception inside error"));
                exception.ShouldBeOfType<DatabaseConnectionException>();
            }
        );
    }
}
