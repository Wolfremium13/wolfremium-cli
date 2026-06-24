namespace Wolfremium.Cli.Database.Backup.Execute.Domain.Models;

public record ColumnMetadata(
    string Name,
    string Type,
    string IsNullable,
    string? DefaultValue
);
