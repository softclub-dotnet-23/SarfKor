# Sarfkor — Backend

ASP.NET Core (.NET 10) + PostgreSQL, Clean Architecture (`Domain` / `Application` / `Infrastructure` / `WebApi`). See the project root `CLAUDE.md` for the full spec.

This backend runs entirely on your own machine — there is no shared dev server. Every teammate runs their own copy against their own local PostgreSQL, using their own secrets. Nothing below goes into git; `appsettings.json` only holds obviously-fake placeholder values, and the real values live in each machine's local user-secrets store (or environment variables in production), per `CLAUDE.md` §2.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 16+ running locally (`localhost:5432`), with a database created for the project
- `dotnet-ef` CLI tool: `dotnet tool install --global dotnet-ef` (any current 10.x version works; CI pins `10.0.0-preview.7.25380.108` but that's not a hard requirement locally)

## First-time setup

1. Create a local database (any Postgres client, e.g. `psql` or a GUI):
   ```sql
   CREATE DATABASE sarfkor;
   ```

2. From `Backend/src/WebApi`, set your own secrets — replace the connection string with your actual local Postgres user/password, and use any long random string for `Jwt:Key` (it only needs to be unpredictable; nobody else's server ever needs to know it, since each of you runs your own):

   **PowerShell:**
   ```powershell
   cd Backend/src/WebApi
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sarfkor;Username=postgres;Password=YOUR_LOCAL_PG_PASSWORD"
   dotnet user-secrets set "Jwt:Issuer" "Sarfkor"
   dotnet user-secrets set "Jwt:Audience" "Sarfkor"
   dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 })))
   ```

   **bash:**
   ```bash
   cd Backend/src/WebApi
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sarfkor;Username=postgres;Password=YOUR_LOCAL_PG_PASSWORD"
   dotnet user-secrets set "Jwt:Issuer" "Sarfkor"
   dotnet user-secrets set "Jwt:Audience" "Sarfkor"
   dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 32)"
   ```

3. (Optional but recommended) Seed yourself an Admin account so the moderation endpoints (`/api/admin/*`) are reachable — without this, the `Admin` role is never assigned to anyone and those endpoints stay permanently 403:
   ```bash
   dotnet user-secrets set "Admin:Email" "you@example.com"
   dotnet user-secrets set "Admin:Password" "SomeStrongPassword123!"
   ```
   The server creates/promotes this account automatically on startup (idempotent — safe to leave set permanently).

4. Apply migrations:
   ```bash
   dotnet ef database update --project ../Infrastructure/Infrastructure.csproj --startup-project WebApi.csproj
   ```

5. Run it:
   ```bash
   dotnet run
   ```
   Listens on `http://localhost:5135` by default (see `Properties/launchSettings.json`). Swagger UI is at `http://localhost:5135/swagger/index.html` in Development.

## Connecting the frontend

`Frontend/` defaults to `http://localhost:5135` and the backend's CORS policy (`appsettings.json` → `Cors:AllowedOrigins`) already allows `http://localhost:5173` and `http://localhost:3000` out of the box — no extra config needed for the standard Vite dev server port.

## Running tests

```bash
dotnet test Backend/tests/Application.Tests/Application.Tests.csproj
```

Application-layer tests use mocks and don't need Postgres running.

## Common issues

- **Fails to connect to the database on startup** — check `ConnectionStrings:DefaultConnection` in your user-secrets matches your actual local Postgres username/password/port, and that Postgres is running (`pg_isready`, or just try connecting with `psql`).
- **`dotnet ef` command not found** — install it globally (see Prerequisites); it's a dev tool, not a project dependency.
- **Everything 401s even with a fresh login** — check `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience` are actually set in your user-secrets (`dotnet user-secrets list` from `Backend/src/WebApi`); without them the app falls back to the placeholder key in `appsettings.json`, which still works for local dev but means tokens aren't unique to your machine.
- **`/api/admin/*` always returns 403** — you haven't set `Admin:Email`/`Admin:Password` (step 3), or you're logged in as an account other than that one.
