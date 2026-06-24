namespace Wolfremium.Cli.Database.Backup.Execute.Application.Contracts;

public interface IBackupProgressIndicator
{
    void ReportProgress(string statusMessage, long bytesExported, double progressPercentage);
}
