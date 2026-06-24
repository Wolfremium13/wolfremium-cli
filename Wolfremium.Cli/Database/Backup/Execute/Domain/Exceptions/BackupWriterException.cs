using System;

namespace Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions;

public class BackupWriterException : Exception
{
    public BackupWriterException(string message) : base(message) { }
    public BackupWriterException(string message, Exception inner) : base(message, inner) { }
}
