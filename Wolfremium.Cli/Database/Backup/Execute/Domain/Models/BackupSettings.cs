using System;
using System.IO;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions;
using Wolfremium.Cli.Database.Shared.Domain.Ports;

namespace Wolfremium.Cli.Database.Backup.Execute.Domain.Models;

public record BackupSettings
{
    private const int MinimumPortNumber = 1;
    private const int MaximumPortNumber = 65535;

    public string Host { get; }
    public int Port { get; }
    public string Username { get; }
    public string Password { get; }
    public string Database { get; }
    public string ExportPath { get; }

    private BackupSettings(string host, int port, string username, string password, string database, string exportPath)
    {
        Host = host;
        Port = port;
        Username = username;
        Password = password;
        Database = database;
        ExportPath = exportPath;
    }

    public static Either<Error, BackupSettings> Create(
        string host, 
        int port, 
        string username, 
        string password,
        string database, 
        string? exportPath, 
        IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(host))
            return Error.New(new ValidationException("Host name cannot be empty."));
        if (port < MinimumPortNumber || port > MaximumPortNumber)
            return Error.New(new ValidationException(
                $"Invalid port number: {port}. Port must be between {MinimumPortNumber} and {MaximumPortNumber}."));
        if (string.IsNullOrWhiteSpace(username))
            return Error.New(new ValidationException("Username cannot be empty."));
        if (string.IsNullOrWhiteSpace(database))
            return Error.New(new ValidationException("Database name cannot be empty."));

        var resolvedPath =
            exportPath ?? Path.Combine(Path.GetTempPath(), $"{host}_{dateTimeProvider.Now:yyyy-MM-dd}_backup.sql");
        return new BackupSettings(host, port, username, password, database, resolvedPath);
    }
}
