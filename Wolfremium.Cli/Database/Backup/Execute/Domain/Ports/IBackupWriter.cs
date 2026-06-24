using System;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;

namespace Wolfremium.Cli.Database.Backup.Execute.Domain.Ports;

public interface IBackupWriter : IAsyncDisposable
{
    Task<Either<Error, Unit>> InitializeAsync(string exportPath);
    Task<Either<Error, Unit>> WriteLineAsync(string line);
    Task<Either<Error, Unit>> WriteLineAsync();
}
