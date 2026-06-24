using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Backup.Execute.Domain.Models;

namespace Wolfremium.Cli.Database.Backup.Execute.Domain.Ports;

public interface IDatabaseService
{
    Task<Either<Error, long>> GetDatabaseSizeAsync(BackupSettings settings);
    Task<Either<Error, IReadOnlyList<string>>> GetServerSettingsAsync(BackupSettings settings);
    Task<Either<Error, IReadOnlyList<RoleInformation>>> GetRolesAsync(BackupSettings settings);
    Task<Either<Error, IReadOnlyList<string>>> GetSchemasAsync(BackupSettings settings);
    Task<Either<Error, IReadOnlyList<string>>> GetTablesAsync(BackupSettings settings);
    Task<Either<Error, IReadOnlyList<ColumnMetadata>>> GetTableColumnsAsync(BackupSettings settings, string tableName);
    Task<Either<Error, IAsyncEnumerable<Dictionary<string, object>>>> GetTableDataAsync(BackupSettings settings, string tableName);
}
