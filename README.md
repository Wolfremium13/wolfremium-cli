# Wolfremium CLI

A fancy, customized command-line tool built using .NET Core and Spectre.Console.

## 🎨 Visual Identity

The CLI features interactive layouts, animated elements, and custom theme styling based on the color palette of [wolfremium.dev](https://www.wolfremium.dev):
*   **Primary Cyan (`#21d789`)**: Used for titles, rule dividers, and successful execution flags.
*   **Secondary Purple (`#7f52ff`)**: Used for layout borders and highlights.
*   **Obsidian Dark Style**: Formatted for standard dark-mode terminal interfaces.

---

## ⚡ Main Features

1.  **System Startup Splash**: Plays a stylized module diagnostics loading sequence on launch.
2.  **Postgres Backup (`postgres_backup`)**: Invokes a custom backup pipeline that creates secure database SQL script backups. Learn more in the [Postgres Backup Guide](docs/postgres-backup.md).
3.  **Connection Tester**: Performs live network checks using Npgsql and prints server version information.
4.  **Session Configuration System**: Allows editing backup settings in-memory during execution or loading defaults automatically from environment variables (`DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASSWORD`, `DB_NAME`, `BACKUP_PATH`).

---

## 🚀 How to Run the CLI

### Natively

Navigate to the project root directory and execute:

```bash
dotnet run --project Wolfremium.Cli/Wolfremium.Cli.csproj
```

### With Docker Compose

The project includes a ready-to-run container network mapping a local PostgreSQL database and the CLI wrapper.

1.  **Start the database container in the background:**
    ```bash
    docker compose up -d db
    ```
2.  **Run the interactive CLI service:**
    ```bash
    docker compose run --rm wolfremium.cli
    ```
3.  **Retrieve backups:**
    Backups generated in the CLI container are written straight to your host's `./backups` folder.
