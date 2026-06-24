using Wolfremium.Cli.Database.Backup.Execute.Application.Contracts;

namespace Wolfremium.Cli.Tests.Database.Backup.Execute.Builders;

public class BackupRequestBuilder
{
    private string _host = "localhost";
    private int _port = 5432;
    private string _username = "postgres";
    private string _password = "mysecretpassword";
    private string _database = "wolfremium_dev";
    private string _exportPath = "backup.sql";

    public BackupRequestBuilder WithHost(string host)
    {
        _host = host;
        return this;
    }

    public BackupRequestBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }

    public BackupRequestBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public BackupRequestBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public BackupRequestBuilder WithDatabase(string database)
    {
        _database = database;
        return this;
    }

    public BackupRequestBuilder WithExportPath(string exportPath)
    {
        _exportPath = exportPath;
        return this;
    }

    public BackupRequest Build()
    {
        return new BackupRequest(_host, _port, _username, _password, _database, _exportPath);
    }
}
