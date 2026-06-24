using System;
using Shouldly;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Exceptions;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Models;
using Xunit;

namespace Wolfremium.Cli.Tests.Database.Configuration.Load.Domain;

public class ApplicationConfigurationTestsShould
{
    [Fact]
    public void CreateWithValidParamsCorrectly()
    {
        var either = ApplicationConfiguration.Create("localhost", 5432, "postgres", "password", "db", "/tmp/backup.sql");
        either.IsRight.ShouldBeTrue();
        either.Match(
            config =>
            {
                config.Host.ShouldBe("localhost");
                config.Port.ShouldBe(5432);
                config.Username.ShouldBe("postgres");
                config.Password.ShouldBe("password");
                config.Database.ShouldBe("db");
                config.ExportPath.ShouldBe("/tmp/backup.sql");
            },
            error => error.ShouldBeNull()
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void FailCreationWhenHostIsEmpty(string? invalidHost)
    {
        var either = ApplicationConfiguration.Create(invalidHost!, 5432, "postgres", "password", "db", null);
        either.IsLeft.ShouldBeTrue();
        either.Match(
            _ => Assert.Fail("Should have failed"),
            error =>
            {
                var exception = error.Exception.Match<Exception>(ex => ex, () => throw new InvalidOperationException("No exception inside error"));
                exception.ShouldBeOfType<ValidationException>();
            }
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void FailCreationWhenPortIsInvalid(int invalidPort)
    {
        var either = ApplicationConfiguration.Create("localhost", invalidPort, "postgres", "password", "db", null);
        either.IsLeft.ShouldBeTrue();
        either.Match(
            _ => Assert.Fail("Should have failed"),
            error =>
            {
                var exception = error.Exception.Match<Exception>(ex => ex, () => throw new InvalidOperationException("No exception inside error"));
                exception.ShouldBeOfType<ValidationException>();
            }
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void FailCreationWhenUsernameIsEmpty(string? invalidUsername)
    {
        var either = ApplicationConfiguration.Create("localhost", 5432, invalidUsername!, "password", "db", null);
        either.IsLeft.ShouldBeTrue();
        either.Match(
            _ => Assert.Fail("Should have failed"),
            error =>
            {
                var exception = error.Exception.Match<Exception>(ex => ex, () => throw new InvalidOperationException("No exception inside error"));
                exception.ShouldBeOfType<ValidationException>();
            }
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void FailCreationWhenDatabaseIsEmpty(string? invalidDatabase)
    {
        var either = ApplicationConfiguration.Create("localhost", 5432, "postgres", "password", invalidDatabase!, null);
        either.IsLeft.ShouldBeTrue();
        either.Match(
            _ => Assert.Fail("Should have failed"),
            error =>
            {
                var exception = error.Exception.Match<Exception>(ex => ex, () => throw new InvalidOperationException("No exception inside error"));
                exception.ShouldBeOfType<ValidationException>();
            }
        );
    }
}
