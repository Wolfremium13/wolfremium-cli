namespace Wolfremium.Cli.Database.Backup.Execute.Domain.Models;

public record RoleInformation(
    string RoleName,
    bool IsSuperUser,
    bool InheritsPrivileges,
    bool CanCreateRole,
    bool CanCreateDatabase,
    bool CanLogin
);
