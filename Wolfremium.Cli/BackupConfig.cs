using System;
using System.IO;

namespace Wolfremium.Cli;

public class BackupConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "mysecretpassword";
    public string Database { get; set; } = "wolfremium_dev";
    private string? _exportPath;
    public string ExportPath
    {
        get => _exportPath ?? Path.Combine(Path.GetTempPath(), $"{Host}_{DateTime.Now:yyyy-MM-dd}_backup.sql");
        set => _exportPath = value;
    }

    public static BackupConfig LoadFromEnv()
    {
        var config = new BackupConfig();

        if (Environment.GetEnvironmentVariable("DB_HOST") is { } host) config.Host = host;
        if (Environment.GetEnvironmentVariable("DB_PORT") is { } portStr && int.TryParse(portStr, out var port)) config.Port = port;
        if (Environment.GetEnvironmentVariable("DB_USER") is { } user) config.Username = user;
        if (Environment.GetEnvironmentVariable("DB_PASSWORD") is { } pass) config.Password = pass;
        if (Environment.GetEnvironmentVariable("DB_NAME") is { } db) config.Database = db;
        if (Environment.GetEnvironmentVariable("BACKUP_PATH") is { } path) config.ExportPath = path;

        return config;
    }

    public string GetConnectionString()
    {
        var passwordPart = string.IsNullOrWhiteSpace(Password) ? "" : $"Password={Password};";
        return $"Host={Host};Port={Port};Username={Username};{passwordPart}Database={Database};Timeout=5;CommandTimeout=5;";
    }
}
