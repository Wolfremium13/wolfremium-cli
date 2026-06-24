using System;
using NSubstitute;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Models;
using Wolfremium.Cli.Database.Shared.Domain.Ports;

namespace Wolfremium.Cli.Tests.Database.Backup.Execute.Builders;

public class BackupSettingsBuilder
{
    private string _host = "localhost";
    private int _port = 5432;
    private string _username = "postgres";
    private string _password = "mysecretpassword";
    private string _database = "wolfremium_dev";
    private string? _exportPath;

    public BackupSettingsBuilder WithHost(string host)
    {
        _host = host;
        return this;
    }

    public BackupSettingsBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }

    public BackupSettingsBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public BackupSettingsBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public BackupSettingsBuilder WithDatabase(string database)
    {
        _database = database;
        return this;
    }

    public BackupSettingsBuilder WithExportPath(string exportPath)
    {
        _exportPath = exportPath;
        return this;
    }

    public BackupSettings Build()
    {
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.Now.Returns(new DateTime(2026, 6, 24));
        var either = BackupSettings.Create(_host, _port, _username, _password, _database, _exportPath, dateTimeProvider);
        return either.Match(
            valid => valid,
            error => throw new InvalidOperationException($"Builder failed to create valid BackupSettings: {error.Message}")
        );
    }
}
