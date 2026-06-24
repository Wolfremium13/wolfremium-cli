using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Exceptions;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Ports;

namespace Wolfremium.Cli.Database.Backup.Execute.Infrastructure.Postgres;

public class FileBackupWriter : IBackupWriter
{
    private StreamWriter? _writer;

    public async Task<Either<Error, Unit>> InitializeAsync(string exportPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(exportPath));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _writer = new StreamWriter(exportPath, false, Encoding.UTF8);
            return Unit.Default;
        }
        catch (Exception ex)
        {
            return Error.New(new BackupWriterException($"Failed to initialize backup writer at {exportPath}", ex));
        }
    }

    public async Task<Either<Error, Unit>> WriteLineAsync(string line)
    {
        try
        {
            if (_writer == null)
            {
                return Error.New(new BackupWriterException("Writer is not initialized."));
            }
            await _writer.WriteLineAsync(line);
            return Unit.Default;
        }
        catch (Exception ex)
        {
            return Error.New(new BackupWriterException("Failed to write line.", ex));
        }
    }

    public async Task<Either<Error, Unit>> WriteLineAsync()
    {
        try
        {
            if (_writer == null)
            {
                return Error.New(new BackupWriterException("Writer is not initialized."));
            }
            await _writer.WriteLineAsync();
            return Unit.Default;
        }
        catch (Exception ex)
        {
            return Error.New(new BackupWriterException("Failed to write empty line.", ex));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_writer != null)
        {
            await _writer.DisposeAsync();
            _writer = null;
        }
    }
}
