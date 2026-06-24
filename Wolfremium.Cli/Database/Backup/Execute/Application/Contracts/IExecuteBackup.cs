using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;

namespace Wolfremium.Cli.Database.Backup.Execute.Application.Contracts;

public interface IExecuteBackup
{
    Task<Either<Error, BackupResult>> ExecuteAsync(BackupCommand command);
}

public record BackupRequest(
    string Host,
    int Port,
    string Username,
    string Password,
    string Database,
    string ExportPath
);

public record BackupCommand(
    BackupRequest Request,
    IBackupProgressIndicator ProgressIndicator
);

public record BackupResult(
    bool Success,
    string Message
);
