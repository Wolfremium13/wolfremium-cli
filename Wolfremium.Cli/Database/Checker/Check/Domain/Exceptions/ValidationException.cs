using System;

namespace Wolfremium.Cli.Database.Checker.Check.Domain.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
