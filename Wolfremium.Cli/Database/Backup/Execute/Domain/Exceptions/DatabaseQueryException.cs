using System;

namespace Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions;

public class DatabaseQueryException : Exception
{
    public DatabaseQueryException(string message) : base(message) { }
    public DatabaseQueryException(string message, Exception inner) : base(message, inner) { }
}
