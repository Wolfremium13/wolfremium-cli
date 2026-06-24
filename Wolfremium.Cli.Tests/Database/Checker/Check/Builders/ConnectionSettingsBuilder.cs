using System;
using Wolfremium.Cli.Database.Checker.Check.Domain.Models;

namespace Wolfremium.Cli.Tests.Database.Checker.Check.Builders;

public class ConnectionSettingsBuilder
{
    private string _host = "localhost";
    private int _port = 5432;
    private string _username = "postgres";
    private string _password = "mysecretpassword";
    private string _database = "wolfremium_dev";

    public ConnectionSettingsBuilder WithHost(string host)
    {
        _host = host;
        return this;
    }

    public ConnectionSettingsBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }

    public ConnectionSettingsBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public ConnectionSettingsBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public ConnectionSettingsBuilder WithDatabase(string database)
    {
        _database = database;
        return this;
    }

    public ConnectionSettings Build()
    {
        var either = ConnectionSettings.Create(_host, _port, _username, _password, _database);
        return either.Match(
            valid => valid,
            error => throw new InvalidOperationException($"Builder failed to create valid ConnectionSettings: {error.Message}")
        );
    }
}
