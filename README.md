# Power Fitness – Gym Management

A web admin console for running a gym: members, trainers, membership plans, class scheduling,
bookings with live capacity, attendance tracking and a revenue dashboard.

**Stack:** ASP.NET Core 9 MVC · EF Core 9 (SQL Server) · ASP.NET Core Identity · Bootstrap 5 · Chart.js
(all front-end assets are self-hosted – no CDNs).

## Features

| Area | What you can do |
| --- | --- |
| **Dashboard** | Active members, revenue vs. last month, attendance rate, next sessions, memberships expiring this week, charts |
| **Members** | Search, paginate, photo (optional), health record + BMI, membership history, recent sessions |
| **Trainers** | Search, filter by specialty, upcoming sessions; deletion never destroys session history |
| **Plans** | Create / edit / activate; price is snapshotted on every membership so revenue history stays correct |
| **Memberships** | Pre-dated memberships, overlap protection, live price/end-date preview, end early |
| **Sessions** | Status tabs, category/search filters, trainer-category rules, trainer double-booking protection |
| **Bookings** | Roster per session; only members whose membership covers the session date can be booked; capacity enforced transactionally; attendance only while the session is running |
| **Accounts** | Cookie login with lockout + rate limiting, role-based access, change password, dark mode |

## Quick start (development)

Prerequisites: [.NET 9 SDK](https://dotnet.microsoft.com/download) and SQL Server (LocalDB, Express, Docker – anything).

```bash
git clone <your repo> && cd GymManagement

# Optional: point at your own SQL Server instead of LocalDB
dotnet user-secrets --project GymManagementPL set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=PowerFitnessGym;User Id=sa;Password=...;TrustServerCertificate=True"

dotnet run --project GymManagementPL
```

On first start in **Development** the app applies migrations, seeds plans, creates two accounts and loads
demo data (trainers, members, sessions, memberships) so every screen has something to show:

| Role | Email | Password |
| --- | --- | --- |
| Super admin | `superadmin@powerfitness.local` | `Dev#Admin123` |
| Admin (front desk) | `admin@powerfitness.local` | `Dev#Admin123` |

These live in `appsettings.Development.json` and are **never used outside Development**.

### Run with Docker

```bash
cp .env.example .env      # then edit the passwords
docker compose up --build  # http://localhost:8080
```

## Configuration

Everything is configured through `appsettings*.json`, user-secrets or environment variables
(`Section__Key` form). **Never commit secrets.**

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string (required; the app refuses to start without it) |
| `Gym:Name` | Brand name shown in the UI |
| `Gym:TimeZone` | IANA/Windows time-zone id. All scheduling and "now" use this zone, not the server's (default `Africa/Cairo`) |
| `Database:AutoMigrate` | Apply EF migrations on startup (default `true`; set `false` if you run migrations in your pipeline) |
| `Seed:SuperAdmin:*`, `Seed:Admin:*` | `Email`, `Password`, `UserName`, `FirstName`, `LastName` of the first login(s). An account with no password configured is skipped |
| `Seed:DemoData` | Sample data – honoured **only** in Development |
| `DataProtection:KeysPath` | Folder to persist cookie-encryption keys (set this in containers, otherwise everyone is logged out on restart) |

### Production checklist

- Set the connection string and `Seed__SuperAdmin__Email/Password` via environment variables or a secret store.
  After first login, change the password (**Account → Password**).
- Serve over HTTPS (the auth cookie is `Secure`; HSTS and a strict CSP are enabled outside Development).
  Behind a reverse proxy set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.
- Persist and back up `GymManagementPL/MembersPictures/` (member photos) and the data-protection keys folder.
- `GET /health` returns 200 when the database is reachable – use it for load-balancer / container health checks.

## Roles

| | Admin | Super admin |
| --- | :---: | :---: |
| Dashboard, sessions, bookings, memberships, view plans | ✅ | ✅ |
| Members, trainers (view/create/edit/delete) | – | ✅ |
| Create / edit / (de)activate plans | – | ✅ |

Permissions are plain `[Authorize(Roles = …)]` attributes on the controllers – easy to change.

## Architecture

```
GymManagementPL   ASP.NET Core MVC: controllers, Razor views, wwwroot, composition root (Program + Extensions/)
      │
GymManagementBLL  Services (business rules), view models + validation, explicit mappers, Result<T>
      │
GymManagementDAL  EF Core DbContext, entities, configurations, migrations, repositories + Unit of Work, seeding
```

Design decisions worth knowing:

- **One clock.** Services read time from an injected `TimeProvider` (the gym's time zone), never `DateTime.Now` –
  which is also what makes the rules unit-testable.
- **Audit columns** (`CreatedAt`/`UpdatedAt`) are stamped in one place: `GymDbContext.SaveChangesAsync`.
- **Membership start date** is stored in the `StartDate` column (entity `CreatedAt`); a membership is active while
  `StartDate <= now < EndDate`.
- **Booking** runs in a serializable transaction wrapped in EF's retrying execution strategy, so two admins can't
  both take the last seat.
- **Results, not exceptions,** for expected business failures (`Result`/`Result<T>`); database unique/FK violations are
  translated to typed exceptions in the Unit of Work.
- **Uploads** live outside `wwwroot` and are served by an authorised action; content is verified by file signature.

## Tests

```bash
dotnet test
```

Service-level tests run against in-memory fake repositories (`GymManagement.Tests/Fakes`) with a controllable clock, and
cover booking/membership/session/trainer/plan/member rules, validation, and the real file-handling code against a
temp directory. Add a test next to the rule whenever you change one.

## Database migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project GymManagementDAL --startup-project GymManagementPL
dotnet ef database update      --project GymManagementDAL --startup-project GymManagementPL
```

`20261004154828_SchemaHardening` upgrades databases created by the original `InitialCreate`: it widens text columns to
`nvarchar`, adds `Memberships.PricePaid` (backfilled from each plan's price), unique plan names, indexes, and switches
session/booking foreign keys from cascade to restrict.

## Upgrading an existing database

- Run the app (or `dotnet ef database update`) – the migration is additive/widening and preserves data.
- Accounts seeded by the old version (`Admin01`, `Admin02`, …) have **no role** and a known password. Change their
  passwords or delete them, then create real accounts through the `Seed:*` settings.
- If the old `appsettings.Production.json` (with the database password) was ever pushed anywhere, **rotate that password**.

See [`docs/IMPROVEMENTS.md`](docs/IMPROVEMENTS.md) for the complete list of fixes and behaviour changes.
