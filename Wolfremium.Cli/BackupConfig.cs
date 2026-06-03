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
    public string ExportPath { get; set; } = Path.Combine(Path.GetTempPath(), "backup.sql"); // Default to system temp folder

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
