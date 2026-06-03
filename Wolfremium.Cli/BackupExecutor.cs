using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Wolfremium.Cli;

public static class BackupExecutor
{
    public static async Task<(bool Success, string Message)> ExecuteBackupAsync(BackupConfig config, Action<string, long, double> onProgress)
    {
        try
        {
            var connString = config.GetConnectionString();
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            onProgress?.Invoke("Connected. Fetching system configurations...", 0, 2.0);

            var sb = new StringBuilder();
            sb.AppendLine($"-- Wolfremium CLI Postgres Backup");
            sb.AppendLine($"-- Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"-- Database: {config.Database}");
            sb.AppendLine();

            // 1. Fetch Postgres Server Configuration Settings
            try
            {
                await using var cmd = new NpgsqlCommand("SELECT name, setting, category, short_desc FROM pg_settings WHERE source = 'configuration file';", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                sb.AppendLine("-- ====================================================");
                sb.AppendLine("-- POSTGRES SERVER CONFIGURATION");
                sb.AppendLine("-- ====================================================");
                while (await reader.ReadAsync())
                {
                    var name = reader.GetString(0);
                    var setting = reader.IsDBNull(1) ? "NULL" : reader.GetString(1);
                    var cat = reader.IsDBNull(2) ? "General" : reader.GetString(2);
                    var desc = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    sb.AppendLine($"-- {name} = '{setting}'  # {cat}: {desc}");
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"-- Warning: Could not dump server configuration settings. {ex.Message}");
                sb.AppendLine();
            }

            onProgress?.Invoke("Fetching role configurations...", 0, 4.0);

            // 2. Fetch Roles
            try
            {
                await using var cmd = new NpgsqlCommand("SELECT rolname, rolsuper, rolinherit, rolcreaterole, rolcreatedb, rolcanlogin FROM pg_roles WHERE rolname NOT LIKE 'pg_%';", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                sb.AppendLine("-- ====================================================");
                sb.AppendLine("-- ROLES CONFIGURATION");
                sb.AppendLine("-- ====================================================");
                while (await reader.ReadAsync())
                {
                    var name = reader.GetString(0);
                    var super = reader.GetBoolean(1);
                    var inherit = reader.GetBoolean(2);
                    var createRole = reader.GetBoolean(3);
                    var createDb = reader.GetBoolean(4);
                    var canLogin = reader.GetBoolean(5);

                    var options = new List<string>();
                    options.Add(super ? "SUPERUSER" : "NOSUPERUSER");
                    options.Add(inherit ? "INHERIT" : "NOINHERIT");
                    options.Add(createRole ? "CREATEROLE" : "NOCREATEROLE");
                    options.Add(createDb ? "CREATEDB" : "NOCREATEDB");
                    options.Add(canLogin ? "LOGIN" : "NOLOGIN");

                    sb.AppendLine("DO $$");
                    sb.AppendLine("BEGIN");
                    sb.AppendLine($"  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '{name}') THEN");
                    sb.AppendLine($"    CREATE ROLE \"{name}\" WITH {string.Join(" ", options)};");
                    sb.AppendLine("  END IF;");
                    sb.AppendLine("END");
                    sb.AppendLine("$$;");
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"-- Warning: Could not dump roles. {ex.Message}");
                sb.AppendLine();
            }

            onProgress?.Invoke("Fetching schema configurations...", 0, 6.0);

            // 3. Fetch Databases Info
            sb.AppendLine("-- ====================================================");
            sb.AppendLine("-- DATABASE CONFIGURATION");
            sb.AppendLine("-- ====================================================");
            sb.AppendLine($"-- CREATE DATABASE \"{config.Database}\";");
            sb.AppendLine();

            // 4. Fetch Schemas
            try
            {
                await using var cmd = new NpgsqlCommand("SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT IN ('information_schema', 'pg_catalog', 'pg_toast');", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                sb.AppendLine("-- ====================================================");
                sb.AppendLine("-- SCHEMAS CONFIGURATION");
                sb.AppendLine("-- ====================================================");
                while (await reader.ReadAsync())
                {
                    var schema = reader.GetString(0);
                    sb.AppendLine($"CREATE SCHEMA IF NOT EXISTS \"{schema}\";");
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"-- Warning: Could not dump schemas. {ex.Message}");
                sb.AppendLine();
            }

            onProgress?.Invoke("Connected. Fetching tables from database...", 0, 10.0);

            // Fetch public tables
            var tables = new List<string>();
            await using (var cmd = new NpgsqlCommand(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name;", conn))
            {
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            if (tables.Count == 0)
            {
                onProgress?.Invoke("No tables found in public schema.", 0, 10.0);
            }
            else
            {
                onProgress?.Invoke($"Found {tables.Count} tables. Starting schema & data dump...", 0, 10.0);
            }

            long totalBytes = 0;
            int tableCount = 0;

            for (int t = 0; t < tables.Count; t++)
            {
                var table = tables[t];
                tableCount++;
                double progressStart = 10.0 + (t * 80.0 / tables.Count);
                double progressSchema = 10.0 + ((t + 0.2) * 80.0 / tables.Count);
                double progressData = 10.0 + ((t + 1.0) * 80.0 / tables.Count);

                onProgress?.Invoke($"Dumping schema for table '{table}' ({tableCount}/{tables.Count})...", totalBytes, progressStart);

                // Fetch columns
                var columns = new List<(string Name, string Type, string IsNullable, string? Default)>();
                await using (var cmd = new NpgsqlCommand(
                    "SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns " +
                    "WHERE table_schema = 'public' AND table_name = @table ORDER BY ordinal_position;", conn))
                {
                    cmd.Parameters.AddWithValue("table", table);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        columns.Add((
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3)
                        ));
                    }
                }

                // Generate CREATE TABLE statement
                sb.AppendLine($"-- Table Structure: {table}");
                sb.AppendLine($"DROP TABLE IF EXISTS \"{table}\" CASCADE;");
                sb.AppendLine($"CREATE TABLE \"{table}\" (");
                for (int i = 0; i < columns.Count; i++)
                {
                    var col = columns[i];
                    var colDef = $"  \"{col.Name}\" {MapDataType(col.Type)}";
                    if (col.IsNullable == "NO") colDef += " NOT NULL";
                    if (col.Default != null) colDef += $" DEFAULT {col.Default}";

                    if (i < columns.Count - 1) colDef += ",";
                    sb.AppendLine(colDef);
                }
                sb.AppendLine(");");
                sb.AppendLine();

                // Dump data using SELECT and generate INSERT statements
                onProgress?.Invoke($"Dumping rows for table '{table}' ({tableCount}/{tables.Count})...", totalBytes, progressSchema);
                await using (var cmd = new NpgsqlCommand($"SELECT * FROM \"{table}\";", conn))
                {
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var cols = new List<string>();
                        var vals = new List<string>();
                        for (int i = 0; i < columns.Count; i++)
                        {
                            cols.Add($"\"{columns[i].Name}\"");
                            vals.Add(FormatSqlValue(reader[columns[i].Name]));
                        }

                        var insert = $"INSERT INTO \"{table}\" ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});";
                        sb.AppendLine(insert);

                        // Increment bytes representation
                        totalBytes += Encoding.UTF8.GetByteCount(insert);
                    }
                }
                sb.AppendLine();
                onProgress?.Invoke($"Table '{table}' finished.", totalBytes, progressData);
            }

            // Write output file
            onProgress?.Invoke("Saving backup SQL file to disk...", totalBytes, 95.0);
            
            // Ensure directory exists
            var dir = Path.GetDirectoryName(Path.GetFullPath(config.ExportPath));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(config.ExportPath, sb.ToString(), Encoding.UTF8);

            onProgress?.Invoke("Backup complete.", totalBytes, 100.0);

            return (true, $"Backup completed successfully. Exported {tables.Count} tables to: {config.ExportPath}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string MapDataType(string postgresType)
    {
        return postgresType.ToUpper() switch
        {
            "CHARACTER VARYING" => "VARCHAR(255)",
            "INTEGER" => "INT",
            "BIGINT" => "BIGINT",
            "TIMESTAMP WITHOUT TIME ZONE" => "TIMESTAMP",
            "TIMESTAMP WITH TIME ZONE" => "TIMESTAMPTZ",
            _ => postgresType
        };
    }

    private static string FormatSqlValue(object val)
    {
        if (val == null || val == DBNull.Value) return "NULL";

        if (val is string s)
        {
            return $"'{s.Replace("'", "''")}'";
        }
        if (val is DateTime dt)
        {
            return $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'";
        }
        if (val is bool b)
        {
            return b ? "TRUE" : "FALSE";
        }
        if (val is double || val is float || val is decimal || val is int || val is long || val is short)
        {
            return val.ToString()!;
        }

        return $"'{val.ToString()!.Replace("'", "''")}'";
    }
}
