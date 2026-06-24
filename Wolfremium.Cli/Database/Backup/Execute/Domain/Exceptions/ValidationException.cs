using System;

namespace Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
