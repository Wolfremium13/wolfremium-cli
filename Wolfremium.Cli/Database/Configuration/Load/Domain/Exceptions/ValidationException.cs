using System;

namespace Wolfremium.Cli.Database.Configuration.Load.Domain.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
