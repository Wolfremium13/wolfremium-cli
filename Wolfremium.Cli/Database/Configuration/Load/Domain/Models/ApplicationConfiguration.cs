using System;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Exceptions;

namespace Wolfremium.Cli.Database.Configuration.Load.Domain.Models;

public record ApplicationConfiguration
{
    private const int MinimumPortNumber = 1;
    private const int MaximumPortNumber = 65535;

    public string Host { get; }
    public int Port { get; }
    public string Username { get; }
    public string Password { get; }
    public string Database { get; }
    public string? ExportPath { get; }

    private ApplicationConfiguration(string host, int port, string username, string password, string database, string? exportPath)
    {
        Host = host;
        Port = port;
        Username = username;
        Password = password;
        Database = database;
        ExportPath = exportPath;
    }

    public static Either<Error, ApplicationConfiguration> Create(string host, int port, string username, string password, string database, string? exportPath)
    {
        if (string.IsNullOrWhiteSpace(host))
            return Error.New(new ValidationException("Host name cannot be empty."));
        if (port < MinimumPortNumber || port > MaximumPortNumber)
            return Error.New(new ValidationException($"Invalid port number: {port}. Port must be between {MinimumPortNumber} and {MaximumPortNumber}."));
        if (string.IsNullOrWhiteSpace(username))
            return Error.New(new ValidationException("Username cannot be empty."));
        if (string.IsNullOrWhiteSpace(database))
            return Error.New(new ValidationException("Database name cannot be empty."));

        return new ApplicationConfiguration(host, port, username, password, database, exportPath);
    }
}
