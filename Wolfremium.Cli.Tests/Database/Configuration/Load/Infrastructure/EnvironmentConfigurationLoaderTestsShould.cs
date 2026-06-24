using System;
using System.Threading.Tasks;
using Shouldly;
using Wolfremium.Cli.Database.Configuration.Load.Infrastructure.Postgres;
using Xunit;

namespace Wolfremium.Cli.Tests.Database.Configuration.Load.Infrastructure;

public class EnvironmentConfigurationLoaderTestsShould
{
    [Fact]
    public async Task LoadDefaultValuesWhenNoEnvironmentVariablesExist()
    {
        ClearEnv();
        var loader = new EnvironmentConfigurationLoader();
        var either = await loader.LoadAsync();

        either.IsRight.ShouldBeTrue();
        either.Match(
            config =>
            {
                config.Host.ShouldBe("localhost");
                config.Port.ShouldBe(5432);
                config.Username.ShouldBe("postgres");
                config.Password.ShouldBe("mysecretpassword");
                config.Database.ShouldBe("wolfremium_dev");
                config.ExportPath.ShouldBeNull();
            },
            error => error.ShouldBeNull()
        );
    }

    [Fact]
    public async Task LoadEnvironmentVariableValuesCorrectly()
    {
        try
        {
            Environment.SetEnvironmentVariable("DB_HOST", "custom_host");
            Environment.SetEnvironmentVariable("DB_PORT", "1234");
            Environment.SetEnvironmentVariable("DB_USER", "custom_user");
            Environment.SetEnvironmentVariable("DB_PASSWORD", "custom_password");
            Environment.SetEnvironmentVariable("DB_NAME", "custom_db");
            Environment.SetEnvironmentVariable("BACKUP_PATH", "/custom/path.sql");

            var loader = new EnvironmentConfigurationLoader();
            var either = await loader.LoadAsync();

            either.IsRight.ShouldBeTrue();
            either.Match(
                config =>
                {
                    config.Host.ShouldBe("custom_host");
                    config.Port.ShouldBe(1234);
                    config.Username.ShouldBe("custom_user");
                    config.Password.ShouldBe("custom_password");
                    config.Database.ShouldBe("custom_db");
                    config.ExportPath.ShouldBe("/custom/path.sql");
                },
                error => error.ShouldBeNull()
            );
        }
        finally
        {
            ClearEnv();
        }
    }

    private void ClearEnv()
    {
        Environment.SetEnvironmentVariable("DB_HOST", null);
        Environment.SetEnvironmentVariable("DB_PORT", null);
        Environment.SetEnvironmentVariable("DB_USER", null);
        Environment.SetEnvironmentVariable("DB_PASSWORD", null);
        Environment.SetEnvironmentVariable("DB_NAME", null);
        Environment.SetEnvironmentVariable("BACKUP_PATH", null);
    }
}
