using System;

namespace Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions;

public class DatabaseSizeException : Exception
{
    public DatabaseSizeException(string message) : base(message) { }
    public DatabaseSizeException(string message, Exception inner) : base(message, inner) { }
}
