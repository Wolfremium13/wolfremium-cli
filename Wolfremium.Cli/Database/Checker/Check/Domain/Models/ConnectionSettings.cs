using System;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Checker.Check.Domain.Exceptions;

namespace Wolfremium.Cli.Database.Checker.Check.Domain.Models;

public record ConnectionSettings
{
    private const int MinimumPortNumber = 1;
    private const int MaximumPortNumber = 65535;

    public string Host { get; }
    public int Port { get; }
    public string Username { get; }
    public string Password { get; }
    public string Database { get; }

    private ConnectionSettings(string host, int port, string username, string password, string database)
    {
        Host = host;
        Port = port;
        Username = username;
        Password = password;
        Database = database;
    }

    public static Either<Error, ConnectionSettings> Create(string host, int port, string username, string password, string database)
    {
        if (string.IsNullOrWhiteSpace(host))
            return Error.New(new ValidationException("Host name cannot be empty."));
        if (port < MinimumPortNumber || port > MaximumPortNumber)
            return Error.New(new ValidationException($"Invalid port number: {port}. Port must be between {MinimumPortNumber} and {MaximumPortNumber}."));
        if (string.IsNullOrWhiteSpace(username))
            return Error.New(new ValidationException("Username cannot be empty."));
        if (string.IsNullOrWhiteSpace(database))
            return Error.New(new ValidationException("Database name cannot be empty."));

        return new ConnectionSettings(host, port, username, password, database);
    }
}
