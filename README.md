# HRMS V1

A Human Resource Management System for one company, built with **Modular Monolithic Architecture** using ASP.NET Core (.NET 8), ASP.NET Core MVC (Controllers & Views) and SQL Server.

Admin and HR operate the system. Employees sign in only to view their own records (profile, attendance, leave, payslips, assets) and change their password.

## Architecture

This project is built using a **Modular Monolithic Architecture**:
- **Single deployable application:** One deployable monolith host (`Hrms.Web`), eliminating distributed system and microservices complexity.
- **Strict module boundaries:** Each business capability is encapsulated inside its own module folder under `src/Modules/` (e.g., `Identity`, `Employees`, etc.).
- **Loose coupling via Contracts:** Modules communicate with each other exclusively through contract interfaces (`Hrms.<Module>.Contracts`), never by directly referencing another module's internal domain or persistence layer.
- **Shared Kernel:** Reusable primitives, result types, and permission constants live in `Hrms.Shared`.
- **Centralized Persistence:** Single database managed via `Hrms.Persistence` with Entity Framework Core 8 code-first migrations.

## Tech stack

- .NET 8, ASP.NET Core MVC (Controllers & Views)
- Modular Monolith Architecture
- SQL Server, EF Core 8 (code-first)
- ASP.NET Core Identity (Guid keys), cookie auth, permission-based authorization
- Pragmatic CQRS, Repository pattern
- FluentValidation

## Modules

Built in this order:

1. Identity (users, roles, permissions)
2. Employees (departments, designations, grades, employees)
3. Settings
4. Audit
5. Calendar (working days, shifts, holidays)
6. Assets
7. Leave
8. Attendance
9. Payroll
10. Reports

## Project layout

```
src/
  Hrms.Web/          Razor Pages UI and host
  Hrms.Shared/       Common building blocks
  Hrms.Persistence/  DbContext, EF configurations, migrations, seed
  Modules/           One folder per module (Contracts + implementation)
tests/               Unit, architecture and integration tests
docs/                Database schema and documentation
```

## Getting started

1. Install the .NET 8 SDK and SQL Server.
2. Set secrets (do not commit them):
   ```
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>" --project src/Hrms.Web
   dotnet user-secrets set "Seed:AdminEmail" "<admin email>" --project src/Hrms.Web
   dotnet user-secrets set "Seed:AdminPassword" "<strong password>" --project src/Hrms.Web
   ```
3. Apply migrations:
   ```
   dotnet ef database update -p src/Hrms.Persistence -s src/Hrms.Web
   ```
4. Run:
   ```
   dotnet run --project src/Hrms.Web
   ```
5. Sign in with the seeded Admin account.

## Documentation

- `docs/hrms_v1_schema.sql`: reference database schema
- `docs/database-documentation`: database documentation
- `AGENTS.md`: architecture rules, business rules and conventions (read before coding)

## Contributing

- Branch from `develop` using `feature/<module>-<task>`.
- Open a pull request; another developer reviews it. Build and architecture tests must pass.
- Only one developer creates EF migrations at a time.
- Never commit secrets (connection strings, passwords, keys).