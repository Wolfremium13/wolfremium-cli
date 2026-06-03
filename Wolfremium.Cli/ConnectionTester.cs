using System;
using System.Threading.Tasks;
using Npgsql;

namespace Wolfremium.Cli;

public static class ConnectionTester
{
    public static async Task<(bool Success, string Message)> TestConnectionAsync(BackupConfig config)
    {
        try
        {
            var connString = config.GetConnectionString();
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            
            await using var cmd = new NpgsqlCommand("SELECT version();", conn);
            var version = await cmd.ExecuteScalarAsync();
            
            return (true, version?.ToString() ?? "Connected successfully.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
