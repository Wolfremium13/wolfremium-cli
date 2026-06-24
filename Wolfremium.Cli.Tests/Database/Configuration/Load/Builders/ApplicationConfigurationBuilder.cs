using System;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Models;

namespace Wolfremium.Cli.Tests.Database.Configuration.Load.Builders;

public class ApplicationConfigurationBuilder
{
    private string _host = "localhost";
    private int _port = 5432;
    private string _username = "postgres";
    private string _password = "mysecretpassword";
    private string _database = "wolfremium_dev";
    private string? _exportPath;

    public ApplicationConfigurationBuilder WithHost(string host)
    {
        _host = host;
        return this;
    }

    public ApplicationConfigurationBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }

    public ApplicationConfigurationBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public ApplicationConfigurationBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public ApplicationConfigurationBuilder WithDatabase(string database)
    {
        _database = database;
        return this;
    }

    public ApplicationConfigurationBuilder WithExportPath(string? exportPath)
    {
        _exportPath = exportPath;
        return this;
    }

    public ApplicationConfiguration Build()
    {
        var either = ApplicationConfiguration.Create(_host, _port, _username, _password, _database, _exportPath);
        return either.Match(
            valid => valid,
            error => throw new InvalidOperationException($"Builder failed to create valid ApplicationConfiguration: {error.Message}")
        );
    }
}
