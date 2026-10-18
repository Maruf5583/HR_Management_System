# AGENTS.md - HRMS V1 (instructions for AI coding agents)

A Human Resource Management System for one company. Admin and HR operate it. Employees sign in only to view their own records and change their password.

> **Read this file fully before writing any code.** Follow it exactly. When a rule here conflicts with your own preference, this file wins. If something is not described here, choose the simplest option that works and keep it consistent with the existing code.

---

## 1. Rules for the AI agent (most important)

### Build what is asked, nothing more

This is a small, single-company, Version 1 product. Keep it simple.

**Scope lock (applies to every task):**

- Build only what the current task asks for and what this file describes. Nothing more.
- Do not add features, pages, buttons, menu items, columns, tables, permissions, roles, settings, packages or refactors that are not written here or in the task.
- Do not touch modules that are not part of the current task. Do not rename, move or reorganize existing files.
- If something is unclear or missing, stop and ask. Do not guess and do not invent.
- When you finish, list every file you created or changed, and remove anything that was not asked for.

**Do NOT add any of these (not in V1):**

- Microservices, message buses, event sourcing, domain events, outbox, sagas
- A separate API project, GraphQL, gRPC, SignalR, background job systems
- Caching layers (Redis, in-memory cache), CDN, health-check dashboards
- Generic repository (`IRepository<T>`), specification pattern, unit-of-work wrappers beyond `DbContext`
- AutoMapper (map manually or with `Select` projections)
- Soft-delete columns, multi-tenancy, localization, theming engines
- Extra tables, extra columns, extra modules, extra enums not described in this file
- Interfaces with only one implementation, unless they are a module `Contracts` interface, a repository interface listed below, or `IFileStore` in `Hrms.Shared`
- Docker orchestration, Kubernetes, CI/CD pipelines (unless asked)
- Dashboard builders, charts, real-time dashboard updates, or any setting that lets a role pick its dashboard

**Do:**

- Follow the folder structure in section 5 exactly.
- Keep each class small and single-purpose.
- Write the minimum code that satisfies the feature. Do not add "future-proof" abstractions.
- Add a unit test for every business rule in section 10.
- Ask before changing the database design (section 6). The schema is fixed.

### Hard architectural rules

1. **Modular monolith.** One deployable app. Each module owns its own tables and code.
2. **A module never uses another module's Domain or Infrastructure.** It may use another module's `Contracts` project only.
3. **No navigation properties between modules.** Store only the other module's id (for example `EmployeeId`). Foreign keys exist in the database, not as C# object links.
4. **No circular dependencies.** Dependency direction: Payroll → Attendance → Leave → Calendar → Employees.
5. **CQRS:** write operations are Commands (use repository). Read operations are Queries (use `HrmsDbContext` directly with `AsNoTracking` and `Select` into DTOs; no repository).
6. **Controllers stay thin.** A controller action only receives input, sends one command or query, and returns a view or a redirect. No business logic in controllers or views.
7. **Never trust ids from the browser for "own data".** For employee self-service pages, always take the employee id from `ICurrentUser.EmployeeId`, never from the URL or form.

---

## 2. Tech stack

| Area | Choice |
|---|---|
| Framework | .NET 8, ASP.NET Core MVC (Controllers and Views) |
| Database | SQL Server |
| ORM | EF Core 8, code-first, one `HrmsDbContext` |
| Auth | ASP.NET Core Identity with `Guid` keys, cookie authentication |
| Authorization | Permission-based policies using role claims |
| Pattern | Modular monolith, CQRS, Repository (command side only) |
| Mediator | `Mediator` (martinothamar, source generator). Do not use MediatR |
| Validation | FluentValidation |
| Logging | Serilog |
| PDF | QuestPDF (payslip) |
| Excel export | ClosedXML (reports) |
| UI | Bootstrap 5, jQuery, DataTables (client-side only where simple) |
| Tests | xUnit, FluentAssertions, NetArchTest.Rules, Testcontainers.MsSql |

Notes:

- .NET 8 support ends 10 Nov 2026. Keep `TargetFramework` and package versions in central files so upgrading to .NET 10 LTS is a small change.
- Keep all EF Core packages on the same 8.x version.

---

## 3. Product scope

### Roles

| Role | Can do |
|---|---|
| Admin | Everything. Also users, roles, permissions, holidays, weekly off days, settings, audit log |
| HR | Employees, attendance, shifts, leave, payroll, assets, reports |
| Employee | View only: own profile, attendance, shift, holidays, leave balance and history, payslips (published only), assets. Change own password |

Admin can add more roles and change what each role can do. The three default roles are system roles and cannot be deleted or renamed. The last active Admin cannot be deactivated, demoted or deleted.

### Out of scope for V1

Biometric devices, employee self-service requests and approval workflows, notifications, forced password change screens, leave policies by grade, carry forward, duty roster, salary components, tax, increments, employee documents, custom dashboard builder, charts, real-time dashboards. Do not build these.

---

## 4. Modules and build order

Build in this order. Do not start a module before the ones it depends on exist (use stubs for `Contracts` interfaces if needed).

| # | Module | Tables owned | Depends on |
|---|---|---|---|
| 1 | **Identity** | AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetRoleClaims, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens | Employees (link by id only) |
| 2 | **Employees** | Departments, Designations, Grades, Employees | none |
| 3 | **Settings** | Settings | none |
| 4 | **Audit** | AuditLogs | none |
| 5 | **Calendar** | CompanyWorkingDays, Shifts, ShiftVersions, ShiftAssignments, Holidays | Employees, Settings |
| 6 | **Assets** | Assets, AssetAssignments | Employees |
| 7 | **Leave** | LeaveTypes, LeaveBalances, LeaveRecords | Employees, Calendar, Settings |
| 8 | **Attendance** | AttendanceDays | Employees, Calendar, Leave, Settings |
| 9 | **Payroll** | Payslips | Employees, Attendance, Leave, Settings |
| 10 | **Reports** | none (read-only): three dashboards and six reports | all modules |

Identity is built first. `AspNetUsers.EmployeeId` starts as a nullable `Guid` column; add the foreign key and unique index when the Employees module exists. The first Admin user is created by the seeder without an employee.

---

## 5. Solution structure

```
Hrms/
├── Hrms.sln
├── global.json                  (pin .NET 8 SDK)
├── Directory.Build.props        (net8.0, Nullable enable, TreatWarningsAsErrors)
├── Directory.Packages.props     (central package versions)
├── README.md                    (short project overview)
├── AGENTS.md                    (this file: instructions for AI agents)
├── docs/
│   ├── hrms_v1_schema.sql       (reference schema; final cross-check for migrations)
│   └── database-documentation
├── src/
│   ├── Hrms.Web/                (MVC UI + host)
│   ├── Hrms.Shared/             (building blocks)
│   ├── Hrms.Persistence/        (HrmsDbContext, EF configurations, migrations, seed)
│   └── Modules/
│       ├── Identity/    Hrms.Identity.Contracts    + Hrms.Identity
│       ├── Employees/   Hrms.Employees.Contracts   + Hrms.Employees
│       ├── Settings/    Hrms.Settings.Contracts    + Hrms.Settings
│       ├── Audit/       Hrms.Audit.Contracts       + Hrms.Audit
│       ├── Calendar/    Hrms.Calendar.Contracts    + Hrms.Calendar
│       ├── Assets/      Hrms.Assets.Contracts      + Hrms.Assets
│       ├── Leave/       Hrms.Leave.Contracts       + Hrms.Leave
│       ├── Attendance/  Hrms.Attendance.Contracts  + Hrms.Attendance
│       ├── Payroll/     Hrms.Payroll.Contracts     + Hrms.Payroll
│       └── Reports/     Hrms.Reports               (queries only, no Domain)
└── tests/
    ├── Hrms.ArchitectureTests/
    ├── Hrms.<Module>.Tests/     (one per module that has business rules)
    └── Hrms.IntegrationTests/   (SQL Server container)
```

### Inside every module (one project, three folders)

```
Hrms.<Module>/
├── Domain/
│   ├── Entities/          entities and aggregates
│   ├── Enums/
│   └── Services/          pure domain rules (for example AttendanceStatusCalculator)
├── Application/
│   ├── Commands/<UseCase>/    Command, Handler, Validator
│   ├── Queries/<UseCase>/     Query, Handler, Dto
│   └── Abstractions/          I<Aggregate>Repository
├── Infrastructure/
│   ├── Repositories/          repository implementations (command side)
│   └── Services/              implementations of Contracts interfaces
└── <Module>Module.cs          AddXModule() service registration
```

### `Hrms.<Module>.Contracts`

Tiny project: public interfaces and DTOs that other modules may use. Examples: `IEmployeeLookup`, `ICalendarQuery`, `ILeaveQuery`, `IAttendanceMarker`, `ISettingsReader`, `IAuditWriter`.

### `Hrms.Shared`

`AuditableEntity` (Created/Updated columns), `Result` and `Result<T>`, `Error`, `PagedResult<T>`, command and query interfaces, `ICurrentUser` (UserId, EmployeeId, permissions), `IClock` (UTC now, convert to Asia/Dhaka), `Permissions` constants, `IFileStore` (save and delete files under the configured upload folder; one local-disk implementation), pipeline behaviors (Validation, Transaction for commands, Audit).

### `Hrms.Persistence`

`HrmsDbContext` (implements the save/commit abstraction directly), `Configurations/<Module>/` (one EF configuration class per entity), an interceptor that fills audit columns, `Migrations/`, `Seed/`.

One database, one `DbContext`. Cross-module foreign keys are configured here only.

### `Hrms.Web`

```
Hrms.Web/
├── Program.cs, appsettings.json
├── Areas/
│   ├── Identity/      Controllers (Account, Users, Roles), Models, Views
│   ├── Employees/     Controllers (Employees, Departments, Designations, Grades), Models, Views
│   ├── Settings/      Controllers, Models, Views
│   ├── Audit/         Controllers, Models, Views
│   ├── Calendar/      Controllers (Shifts, Holidays, WorkingDays), Models, Views
│   ├── Assets/        Controllers, Models, Views
│   ├── Leave/         Controllers (Types, Records, Balances), Models, Views
│   ├── Attendance/    Controllers, Models, Views
│   ├── Payroll/       Controllers, Models, Views
│   └── Reports/       Controllers (DashboardController with Admin, Hr and Employee actions; report controllers), Models, Views
├── Controllers/       HomeController (sends each user to the right dashboard, error)
├── Views/             Home, Shared (_Layout, _Sidebar, toast, pager, validation scripts)
├── Models/            Shared view models only
├── Infrastructure/    CurrentUser, PermissionPolicyProvider, PermissionHandler, global exception handler
└── wwwroot/
```

Inside each Area: `Controllers/`, `Models/` (view models), `Views/<ControllerName>/`.

An Area equals a Module. Module projects never contain controllers or views.

---

## 6. Database (fixed design)

SQL Server, 25 tables. The file `docs/hrms_v1_schema.sql` is the **source of truth**. Entities and EF configurations must produce the same schema. After generating a migration, compare it with that file. Do not add, remove or rename tables or columns without being asked.

### Conventions

| Topic | Convention |
|---|---|
| Primary keys | `uniqueidentifier`, default `NEWSEQUENTIALID()` |
| Audit columns | `CreatedAtUtc`, `CreatedByUserId`, `UpdatedAtUtc`, `UpdatedByUserId` on master and transaction tables. Filled automatically by an interceptor |
| Time | All timestamps UTC `datetime2(3)`. `WorkDate` is the company local date (Asia/Dhaka) |
| Money | `decimal(12,2)` |
| Status values | `nvarchar` with a `CHECK` constraint. Map C# enums with `HasConversion<string>()` |
| History | Never hard-delete employees, attendance, leave, payslips, assignments. Use status and effective dates |
| Current record | `EffectiveTo IS NULL` or `ReturnedAtUtc IS NULL` marks the active row (filtered unique index) |
| Concurrency | `RowVersion` on Employees, AttendanceDays, LeaveBalances, Payslips, Assets |
| `...ByUserId` columns | Store a user id but are not foreign keys |
| Delete rules | `NO ACTION`, except Identity child tables (cascade) |
| National ID | Stored encrypted by the application (`NidNumberEncrypted`) |

### Tables by module

**Identity (standard ASP.NET Core Identity, Guid keys, plus custom columns)**

- `AspNetUsers` custom: `EmployeeId` (nullable, unique), `Status` (Active, Inactive), `LastLoginAtUtc`, audit columns
- `AspNetRoles` custom: `Description`, `IsSystem`, audit columns
- `AspNetRoleClaims`: one row per permission per role, `ClaimType = 'permission'`, unique `(RoleId, ClaimType, ClaimValue)`
- Others standard: `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`

**Employees**

- `Departments`: Name (unique), Status
- `Designations`: Title (unique per department), DepartmentId, Status
- `Grades`: DesignationId, Level (unique per designation), Code (unique), Salary (>= 0), Status
- `Employees`: EmployeeCode (unique), FirstName, LastName, Email (unique when present), Phone, DateOfBirth, Gender, Address, NidNumberEncrypted, PhotoKey (file key of the profile photo under wwwroot/uploads, nullable), JoinDate, EmploymentType (Permanent, Probation, Contract, Intern), DepartmentId, DesignationId, GradeId, EmergencyContactName, EmergencyContactPhone, Status (Active, OnNotice, Resigned, Terminated), ExitDate (not before JoinDate), RowVersion
- **Composite foreign keys:** `(DesignationId, DepartmentId)` must exist in Designations, and `(GradeId, DesignationId)` must exist in Grades. This stops an employee from having a designation outside their department or a grade outside their designation.

**Settings**: `Settings` (SettingKey unique, ValueText, Description, UpdatedAtUtc, UpdatedByUserId)

**Audit**: `AuditLogs` (bigint identity, UserId, Action, EntityName, EntityId, OldValues JSON, NewValues JSON, IpAddress, CreatedAtUtc). Insert-only.

**Calendar**

- `CompanyWorkingDays`: always 7 rows, `DayOfWeek` 0 (Sunday) to 6, `IsWorkingDay`. Seed: Friday and Saturday off
- `Shifts`: Name (unique), Status
- `ShiftVersions`: ShiftId, StartTime, EndTime (earlier than StartTime means night shift), GraceMinutes, BreakMinutes, EffectiveFrom, EffectiveTo (NULL means current). One current version per shift
- `ShiftAssignments`: EmployeeId, ShiftId, EffectiveFrom, EffectiveTo. One current assignment per employee
- `Holidays`: Name, HolidayDate (unique), Type (Government, Company)

**Assets**

- `Assets`: AssetTag (unique), Name, Category (Laptop, Monitor, Phone, SIM, IDCard, Other), SerialNumber (unique when present), PurchaseDate, Condition (New, Good, Fair, Damaged), Status (Available, Assigned, InRepair, Retired), Notes, RowVersion
- `AssetAssignments`: AssetId, EmployeeId, AssignedAtUtc, ReturnedAtUtc (NULL means still held), Note. One open assignment per asset

**Leave**

- `LeaveTypes`: Name (unique), Code (unique), IsPaid, DaysPerYear (>= 0), RequiresDocument, Status. Unpaid types use a large `DaysPerYear` (for example 365) so `Used <= Entitled` still holds
- `LeaveBalances`: EmployeeId, LeaveTypeId, Year, Entitled, Used, `Remaining` (computed, persisted: Entitled minus Used). Unique `(EmployeeId, LeaveTypeId, Year)`. `0 <= Used <= Entitled`
- `LeaveRecords`: EmployeeId, LeaveTypeId, StartDate, EndDate, IsHalfDay, TotalDays (> 0, excludes weekends and holidays), Reason, AttachmentKey, Status (Recorded, Cancelled). A half day requires StartDate = EndDate and TotalDays = 0.5

**Attendance**

- `AttendanceDays`: EmployeeId, WorkDate, ShiftVersionId (nullable), CheckInUtc, CheckOutUtc (not before check-in), LateMinutes, WorkedMinutes, Status, Note, RowVersion. Unique `(EmployeeId, WorkDate)`
- Status values: `Present`, `Late`, `Absent`, `HalfDay`, `OnLeave`, `Holiday`, `Weekend`

**Payroll**

- `Payslips`: EmployeeId, Month (always the 1st), BaseSalary, PerDayDivisor, PerDayRate, WorkingDays, PresentDays, UnpaidLeaveDays, AbsentDays, LeaveCut, OtherDeductions, DeductionNote, NetPay, Status (Draft, Published), PublishedAtUtc, PublishedByUserId, RowVersion. Unique `(EmployeeId, Month)`. Published requires a publish date

### Seed data (seeder runs on startup in Development, and by a command in Production)

- Roles: Admin, HR, Employee (all `IsSystem = 1`) with their permissions
- `CompanyWorkingDays`: 7 rows (Friday and Saturday off)
- `Settings` keys:

| Key | Seed value | Meaning |
|---|---|---|
| `company.name` | Company Name | Company name |
| `company.timezone` | Asia/Dhaka | Time zone for WorkDate |
| `attendance.half_day_hours` | 4 | Fewer worked hours count as half day |
| `attendance.holiday_priority` | Holiday | Status when a holiday falls on a weekly off day (Holiday or Weekend) |
| `leave.year_start` | 01-01 | Leave year start (MM-dd) |
| `payroll.per_day_divisor` | 30 | Divisor for per-day rate |
| `employee.code_format` | EMP-{0:0000} | Employee code format |

- First Admin user: created by the seeder using the ASP.NET Core Identity password hasher. Email and password come from configuration (user-secrets or environment variables). Never hard-code a password.

---

## 7. Identity and permissions

- `ApplicationUser : IdentityUser<Guid>`, `ApplicationRole : IdentityRole<Guid>`.
- Self-registration is disabled. Only Admin creates users. Build only Login, Logout, Change password, Access denied. Do not scaffold the full default Identity UI.
- Login is allowed only when `Status = Active`. Lockout is enabled (5 failed attempts). When a user is deactivated, update the security stamp so existing cookies stop working.
- The cookie holds role claims, permission claims, and the user's `EmployeeId` claim.
- One custom `IAuthorizationPolicyProvider` creates a policy for any permission name, so `[Authorize(Policy = "payroll.publish")]` works with no per-permission registration.
- Permission constants live in one static class `Permissions` in `Hrms.Shared` (for example `Permissions.Payroll.Publish`).
- Admin ticks permissions on a role page. Each tick adds a row to `AspNetRoleClaims`; each untick removes it. The Admin role is locked and keeps all permissions.
- Hide menu items and buttons the user lacks permission for (UX only). Real protection is the policy on every controller action.

### Permission list

| Module | Permissions |
|---|---|
| Users and roles | `users.manage`, `roles.manage` |
| Employees | `employees.view`, `employees.manage`, `employees.view_nid`, `employees.view_own` |
| Attendance | `attendance.view_own`, `attendance.view_all`, `attendance.manage` |
| Shifts | `shifts.manage`, `shifts.view_own` |
| Holidays | `holidays.manage`, `holidays.view` |
| Leave | `leave.view_own`, `leave.view_all`, `leave.manage` |
| Payroll | `payroll.view_own`, `payroll.view_all`, `payroll.generate`, `payroll.publish` |
| Assets | `assets.view_own`, `assets.manage` |
| Reports | `reports.view` |
| Settings | `settings.manage` |
| Audit | `audit.view` |

### Default role permissions

| Role | Permissions |
|---|---|
| Admin | All |
| HR | All except `users.manage`, `roles.manage`, `holidays.manage`, `settings.manage`, `audit.view` |
| Employee | `employees.view_own`, `attendance.view_own`, `shifts.view_own`, `holidays.view`, `leave.view_own`, `payroll.view_own`, `assets.view_own` |

The National ID is visible only to users with `employees.view_nid`. Employees see only **Published** payslips and only their own.

---

## 8. CQRS and repository rules (keep it simple)

- A **Command** changes data. Flow: Controller action → `Mediator.Send(command)` → Validation behavior → Transaction behavior → Handler → repository → `SaveChanges` → `Result`.
- A **Query** reads data. The handler uses `HrmsDbContext` with `AsNoTracking()` and `Select` into a DTO. No repository, no entities returned to controllers or views.
- **Repository:** one interface per aggregate that actually needs one, with only the methods that are used (for example `GetByIdAsync`, `Add`). No generic repository.
- **Result:** handlers return `Result` or `Result<T>`. Do not use exceptions for expected business failures (for example "balance too low"). Use exceptions only for unexpected errors.
- **Transactions:** every command runs inside one transaction. Where an overlap check is needed, run it inside the transaction with `UPDLOCK, HOLDLOCK`.
- **Audit:** commands for important Admin and HR actions write an `AuditLogs` row (create, update, delete, publish, role change) with old and new values as JSON. Never write password hashes, security stamps or the National ID value into the audit JSON (log only that the field changed).
- **Concurrency:** on `DbUpdateConcurrencyException`, return a friendly "someone else changed this record, reload and try again" message.
- **DTOs and view models:** controllers bind to a view model, never to a domain entity.

---

## 9. MVC rules (Controllers and Views)

- One controller per feature inside its Area. Typical actions: `Index` (list with search, filter, paging), `Create`, `Edit`, `Details`.
- A GET action sends a Query and returns a view with a view model. A POST action sends a Command, then redirects (Post-Redirect-Get). On failure, add errors to `ModelState` and return the same view.
- Use view models for forms and lists. Never pass domain entities to views.
- Show success and error messages with `TempData` toasts.
- Use `[ValidateAntiForgeryToken]` on every POST action (or a global auto-validate filter).
- Edit forms carry the `RowVersion` as a hidden field.
- Every controller or action has `[Authorize(Policy = Permissions....)]`.
- Server-side paging for large lists (Employees, Attendance, Payslips, Audit).
- Show times in Asia/Dhaka. Store UTC.
- Cookie settings: `HttpOnly`, `Secure`, `SameSite=Lax`. Enable HTTPS and HSTS. Persist Data Protection keys (file or database) so restarts do not break cookies or encrypted NIDs.

---

## 10. Business rules (implement and unit test every one)

### Attendance

HR enters attendance by hand each day. `AttendanceDays` has one row per employee per day. The status is set by the **first matching rule**, in this order:

1. The date is a holiday or a weekly off day → `Holiday` or `Weekend`. If a holiday falls on a weekly off day, setting `attendance.holiday_priority` decides which one.
2. A Recorded leave covers the day → `OnLeave`
3. No check-in → `Absent`
4. Worked fewer hours than `attendance.half_day_hours` → `HalfDay`
5. Check-in later than shift start plus grace minutes → `Late`
6. Otherwise → `Present`

- `LateMinutes` = minutes after (shift start + grace). `WorkedMinutes` = check-out minus check-in minus break minutes.
- `ShiftVersionId` is the shift version in force that day, found through `ShiftAssignments` and `ShiftVersions`. It is NULL on weekends, holidays and leave days.
- The daily sheet lists every active employee. Rows for leave, holidays and weekends are prefilled. Saving the sheet is one bulk command in one transaction.
- Editing a record requires a note.

### Shifts and calendar

- Changing shift times **adds a new `ShiftVersions` row and closes the old one**. Never edit a version that attendance already uses.
- Changing an employee's shift closes the current `ShiftAssignments` row and adds a new one, after an overlap check.
- A shift cannot be deactivated while any employee has it as a current assignment.
- Weekly off day changes apply from today onward and cannot change a month whose payslips are Published.
- A past holiday cannot be deleted or moved. When a future holiday changes, recalculate attendance rows for that date.
- Admin manages weekly off days and holidays. Admin and HR manage shifts and assignments.

### Leave

- HR records leave for employees. Every employee gets the same `DaysPerYear` per leave type each year. Unused days do not carry forward.
- `TotalDays` excludes weekends and holidays inside the date range.
- **Record leave** (one transaction): check no overlap with other Recorded leave, check the balance is enough, save the leave record, increase `Used`, and mark the attendance days `OnLeave`.
- **Cancel leave** (one transaction): set status Cancelled, decrease `Used`, recalculate the attendance days.
- **Start new leave year:** create a balance for every active employee and leave type using `DaysPerYear`. Old balances stay for history.
- When an employee is added, create their balances for the current year from the leave types.

### Payroll

- One payslip per employee per month. Generation cannot run twice for the same month (unique key plus transaction).
- On generate, copy `BaseSalary` from the grade, `PerDayDivisor` from settings, then calculate:
  - `PerDayRate = BaseSalary / PerDayDivisor`
  - `LeaveCut = (UnpaidLeaveDays + AbsentDays) * PerDayRate`
  - `NetPay = BaseSalary - LeaveCut - OtherDeductions`
- Copying salary, divisor and rate means later grade or setting changes never alter old payslips.
- HR can add `OtherDeductions` with a note while the payslip is Draft.
- A Published payslip is locked and visible to the employee. Publish sets `PublishedAtUtc` and `PublishedByUserId`.
- The PDF is generated on download. Do not store PDF files.

### Assets

- An asset can be assigned only when its status is `Available`. Assign creates an `AssetAssignments` row and sets the asset to `Assigned`.
- Return sets `ReturnedAtUtc` with a condition note. The asset becomes `Available`, or `InRepair` if damaged.
- Assign and return update `AssetAssignments` and `Assets.Status` in one transaction.

### Dashboards and reports (Reports module)

There are exactly **three dashboards**: Admin, HR and Employee. Each has its own page and its own view model. Build them only when a task asks, and add a widget only after the module that supplies its data exists.

**Which dashboard a user sees** (one rule, in one place, used by `HomeController` after sign-in):

1. The user is in the **Admin** role: Admin dashboard.
2. Otherwise, the user has the `reports.view` permission: HR dashboard.
3. Otherwise: Employee dashboard.

This also decides the dashboard for **every new custom role**, with no extra setup: give the role `reports.view` to get the HR dashboard, leave it out to get the Employee dashboard. Do not add a column, a setting or a permission for choosing a dashboard.

**Protection:** the Admin dashboard is allowed only for the Admin role, the HR dashboard only with `reports.view`, and the Employee dashboard for any signed-in user. A user who opens another dashboard's URL gets Access denied.

**Widgets show only when the user has the widget's permission.** A widget whose module is not built yet is not shown.

| Dashboard | Widget | Permission |
|---|---|---|
| Admin | Employee count by status | `employees.view` |
| Admin | Active and inactive users, number of roles | `users.manage` |
| Admin | Today's attendance summary | `attendance.view_all` |
| Admin | Last 10 audit log entries | `audit.view` |
| Admin | Asset counts by status | `assets.manage` |
| HR | Today's attendance (Present, Late, Absent, On leave) and whether today's attendance has been entered | `attendance.view_all` |
| HR | Headcount, joined this month, employees on notice | `employees.view` |
| HR | Employees on leave today and in the next 7 days | `leave.view_all` |
| HR | This month's payroll status (not generated, Draft, Published) | `payroll.view_all` |
| HR | Asset counts by status | `assets.manage` |
| HR | Upcoming holidays | `holidays.view` |
| Employee | This month's attendance counts | `attendance.view_own` |
| Employee | Leave balance by type | `leave.view_own` |
| Employee | Latest published payslip | `payroll.view_own` |
| Employee | Current shift | `shifts.view_own` |
| Employee | My assets | `assets.view_own` |
| Employee | Upcoming holidays | `holidays.view` |
| Employee | Profile summary | `employees.view_own` |

- The Employee dashboard uses `ICurrentUser.EmployeeId` only. If the user has no linked employee, show a friendly message instead of widgets.
- Dashboards use simple cards and small tables. No charts, no auto-refresh.
- Each dashboard widget is a query (`AsNoTracking`, DTO). Dashboards never change data.

**Reports** (permission `reports.view`, Admin and HR): Daily attendance, Monthly attendance, Late report, Leave report, Payroll register, Asset report. Each has filters and an Excel export (ClosedXML). Reports are read-only queries over the other modules' tables.

### Employees and users

- Adding an employee: HR creates the employee. Assigning a shift and creating leave balances are added later, when the Calendar and Leave modules exist. Admin creates the login separately.
- Resigned or Terminated employees are kept, never deleted.

### Employee profile photo

- Optional. One photo per employee. Stored as a file under `wwwroot/uploads/employees/<EmployeeId>/<Guid>.<ext>` (upload root comes from configuration `FileStorage:RootPath`, default `wwwroot/uploads`). Only the file key is saved in `Employees.PhotoKey`. The folder is excluded from Git (`.gitignore`) and must not be overwritten on deploy.
- Allowed types: `.jpg`, `.jpeg`, `.png`. Maximum size: **1 MB (1,048,576 bytes)**. Reject anything else with a clear message.
- Validate on the server: extension, size, **and the file signature** (JPEG starts with `FF D8 FF`, PNG starts with `89 50 4E 47 0D 0A 1A 0A`). Do not trust the file name or the browser content type.
- Save with a generated name. Never use the uploaded file name. Replacing or removing a photo deletes the old file.
- Limit the upload request size on the upload action (about 1.2 MB) so large files are rejected early.
- Photos are served as static files from `wwwroot`. The image URL has no permission check, so keep file names as random GUIDs. Do not allow script or executable files in the upload folder.
- Upload, replace and remove require `employees.manage`. Showing the photo follows the page it appears on (employee details, My profile).
- No image resizing, cropping or extra image library. Without a photo, show initials (CSS only).

---

## 11. Module Contracts (cross-module access)

Define these in the `Contracts` projects first so two developers can work in parallel. Use a stub implementation until the real module exists.

| Interface | Provided by | Purpose |
|---|---|---|
| `IEmployeeLookup` | Employees | Employee exists, name, active status, grade salary |
| `ISettingsReader` | Settings | Read a setting value by key |
| `ICalendarQuery` | Calendar | Is working day, is holiday, shift version for an employee and date |
| `ILeaveQuery` | Leave | Is an employee on leave on a date, unpaid leave days in a month |
| `IAttendanceMarker` | Attendance | Mark or recalculate attendance days when leave is recorded or cancelled |
| `IAttendanceQuery` | Attendance | Present and absent day counts for payroll |
| `IAuditWriter` | Audit | Write an audit log entry |

---

## 12. Naming and code style

- Namespace equals folder path, for example `Hrms.Leave.Application.Commands.RecordLeave`.
- Command `RecordLeaveCommand`, handler `RecordLeaveCommandHandler`, validator `RecordLeaveCommandValidator`.
- Query `GetDailySheetQuery`, result `DailySheetDto`.
- Entities are plural in the database (`Employees`), singular in C# (`Employee`).
- Async methods end with `Async`. Pass `CancellationToken` through handlers.
- Nullable reference types enabled. Warnings as errors.
- No magic strings for permissions, settings keys or statuses. Use constants and enums.
- Prefer `record` for commands, queries and DTOs.

---

## 13. Testing

- **Unit tests** for every rule in section 10: attendance status order, late and worked minutes, leave day counting, overlap detection, balance checks, payslip calculation, asset assign and return rules, last-Admin rule.
- **Architecture tests** (`NetArchTest`), run in CI: a module does not reference another module's Domain or Infrastructure, and Domain does not reference Infrastructure.
- **Integration tests** (SQL Server container): migration applies, filtered unique indexes work, composite foreign keys reject bad combinations, concurrency token works.

---

## 14. Getting started

1. Install the .NET 8 SDK and SQL Server (local or Docker).
2. Set the connection string and first Admin credentials in user-secrets:
   - `ConnectionStrings:DefaultConnection`
   - `Seed:AdminEmail`, `Seed:AdminPassword`
3. Apply migrations:
   `dotnet ef database update -p src/Hrms.Persistence -s src/Hrms.Web`
4. Run: `dotnet run --project src/Hrms.Web`
5. Sign in with the seeded Admin account.

Create a migration: `dotnet ef migrations add <Name> -p src/Hrms.Persistence -s src/Hrms.Web`. Only one developer creates migrations at a time.

---

## 15. How to build each module (repeat for every module)

1. Create a feature branch from `develop`.
2. Write the `Contracts` interfaces other modules need.
3. Write the Domain entities and rules.
4. Write the EF configurations in `Hrms.Persistence` (indexes, CHECK constraints, composite foreign keys, `RowVersion`).
5. Create the migration and compare it with `docs/hrms_v1_schema.sql`.
6. Write commands, validators, handlers and repositories.
7. Write queries and DTOs.
8. Write the controllers, view models and views with permission policies.
9. Write tests.
10. Open a pull request. Build and architecture tests must pass.

Build one small feature at a time, not a whole module at once.

### Definition of done

- All screens work and are protected by permission policies
- Business rule tests pass, architecture tests pass
- Migration matches the schema document
- Manually tested as Admin, HR and Employee (Employee sees only own data)
- No code or tables added beyond this file

---

## 16. Quick checklist before you write code

- Is this feature described in this file? If not, do not build it.
- Does it stay inside its module and use only `Contracts` of other modules?
- Is the controller action thin?
- Does a command use a transaction and return a `Result`?
- Does a query use `AsNoTracking` and a DTO?
- Is it the simplest version that works?
- Did I add anything that was not asked for (feature, page, button, column, table, permission, package)? If yes, remove it.
