using System;
using Shouldly;
using Wolfremium.Cli.Database.Checker.Check.Domain.Exceptions;
using Wolfremium.Cli.Database.Checker.Check.Domain.Models;
using Xunit;

namespace Wolfremium.Cli.Tests.Database.Checker.Check.Domain;

public class ConnectionSettingsTestsShould
{
    [Fact]
    public void CreateWithValidParamsCorrectly()
    {
        var either = ConnectionSettings.Create("localhost", 5432, "postgres", "password", "db");
        either.IsRight.ShouldBeTrue();
        either.Match(
            settings =>
            {
                settings.Host.ShouldBe("localhost");
                settings.Port.ShouldBe(5432);
                settings.Username.ShouldBe("postgres");
                settings.Password.ShouldBe("password");
                settings.Database.ShouldBe("db");
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
        var either = ConnectionSettings.Create(invalidHost!, 5432, "postgres", "password", "db");
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
        var either = ConnectionSettings.Create("localhost", invalidPort, "postgres", "password", "db");
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
        var either = ConnectionSettings.Create("localhost", 5432, invalidUsername!, "password", "db");
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
        var either = ConnectionSettings.Create("localhost", 5432, "postgres", "password", invalidDatabase!);
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
