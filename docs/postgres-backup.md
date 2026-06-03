# Postgres Backup Guide (`postgres_backup`)

The **postgres_backup** feature in the Wolfremium CLI is a robust utility designed to back up PostgreSQL databases securely and display real-time progress indicators using a customized terminal interface.

## ⚙️ Technical Design

Backups are executed natively inside the C# application via the `Npgsql` database driver. This eliminates the need for any local command-line client installations (like `pg_dump` or docker instances) on the host machine.

### Backup Process Flow:
1. **Metadata Inspection**: The CLI connects to the target database and queries PostgreSQL system catalogs (`information_schema.tables` and `information_schema.columns`) to discover public tables, columns, constraints, and data types.
2. **Schema Reconstruction**: It dynamically generates standard SQL structure statements (`DROP TABLE IF EXISTS ...` and `CREATE TABLE ...`).
3. **Data Streaming**: It opens a streaming query (`SELECT * FROM table`) on each table, sequentially generating formatted SQL `INSERT` statements for every row and piping the output directly to the destination backup file.

## 📊 Live Dashboard & Display

During active backup runs, the CLI opens a Spectre.Console `LiveDisplay` panel containing:
*   **Active Status**: The current table schema or data rows being exported.
*   **Real-time Metrics**: Total duration (elapsed time) and bytes written (transferred size in MB).
*   **Progress Reference**: Before starting the backup, the CLI runs a query to fetch the database size (`pg_database_size`). It uses this value to display a percentage bar (`[██████░░░░░░░░] 42%`) indicating raw progress relative to database capacity.
*   **Log Feed**: A scrolling feed of the tables currently being processed.

## 🔒 Configuration

Configurations can be read from environment variables or edited in-memory for the current session.

### Environment Variables

| Env Variable | Description | Default Value |
| :--- | :--- | :--- |
| `DB_HOST` | Hostname / IP of your PG server | `localhost` |
| `DB_PORT` | Port of your PG server | `5432` |
| `DB_USER` | Connection username | `postgres` |
| `DB_PASSWORD` | Connection password | `""` (empty) |
| `DB_NAME` | Database to backup | `postgres` |
| `BACKUP_PATH` | Target output backup path | `backup.sql` |
