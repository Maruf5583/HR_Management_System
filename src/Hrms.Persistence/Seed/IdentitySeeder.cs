using Hrms.Calendar.Domain.Entities;
using System.Security.Claims;
using Hrms.Employees.Domain.Entities;
using Hrms.Employees.Domain.Enums;
using Hrms.Identity.Domain.Entities;
using Hrms.Identity.Domain.Enums;
using Hrms.Settings.Contracts;
using Hrms.Settings.Domain.Entities;
using Hrms.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hrms.Persistence.Seed;

public class IdentitySeeder
{
    private const string PermissionClaimType = "permission";

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HrmsDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        HrmsDbContext dbContext,
        IConfiguration configuration,
        ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedSettingsAsync();
        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedEmployeesAndLinkUsersAsync();
        await SeedWorkingDaysAsync();
    }

    private async Task SeedRolesAsync()
    {
        var systemRoles = new[]
        {
            (Name: ApplicationRole.AdminRoleName, Description: "System Administrator", Permissions: Permissions.AdminDefault),
            (Name: ApplicationRole.HrRoleName, Description: "Human Resources Manager", Permissions: Permissions.HrDefault),
            (Name: ApplicationRole.EmployeeRoleName, Description: "Company Employee", Permissions: Permissions.EmployeeDefault)
        };

        foreach (var (name, description, permissions) in systemRoles)
        {
            var role = await _roleManager.FindByNameAsync(name);
            if (role == null)
            {
                role = new ApplicationRole
                {
                    Name = name,
                    Description = description,
                    IsSystem = true
                };

                var createResult = await _roleManager.CreateAsync(role);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}",
                        name, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }
            }
            else
            {
                if (!role.IsSystem)
                {
                    role.IsSystem = true;
                    await _roleManager.UpdateAsync(role);
                }
            }

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var existingClaimValues = existingClaims
                .Where(c => c.Type == PermissionClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var permission in permissions)
            {
                if (!existingClaimValues.Contains(permission))
                {
                    await _roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, permission));
                }
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        var seedUsers = new[]
        {
            (
                Email: _configuration["Seed:AdminEmail"] ?? "admin@hrms.local",
                Password: _configuration["Seed:AdminPassword"] ?? "Admin@123456",
                Role: ApplicationRole.AdminRoleName
            ),
            (
                Email: _configuration["Seed:HrEmail"] ?? "hr@hrms.local",
                Password: _configuration["Seed:HrPassword"] ?? "Hr@123456",
                Role: ApplicationRole.HrRoleName
            ),
            (
                Email: _configuration["Seed:EmployeeEmail"] ?? "employee@hrms.local",
                Password: _configuration["Seed:EmployeePassword"] ?? "Employee@123456",
                Role: ApplicationRole.EmployeeRoleName
            )
        };

        foreach (var (email, password, role) in seedUsers)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                continue;
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                if (!await _userManager.IsInRoleAsync(existingUser, role))
                {
                    await _userManager.AddToRoleAsync(existingUser, role);
                }
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Status = UserStatus.Active,
                LockoutEnabled = true,
                EmployeeId = null
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                _logger.LogInformation("Successfully created and seeded user: {Email} ({Role})", email, role);
            }
            else
            {
                _logger.LogError("Failed to create seed user {Email}: {Errors}",
                    email, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedEmployeesAndLinkUsersAsync()
    {
        // 1. Seed or retrieve organizational hierarchy
        var adminDept = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Name == "Executive Management");
        if (adminDept == null)
        {
            adminDept = Department.Create("Executive Management");
            await _dbContext.Departments.AddAsync(adminDept);
            await _dbContext.SaveChangesAsync();
        }

        var hrDept = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Name == "Human Resources");
        if (hrDept == null)
        {
            hrDept = Department.Create("Human Resources");
            await _dbContext.Departments.AddAsync(hrDept);
            await _dbContext.SaveChangesAsync();
        }

        var engDept = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Name == "Engineering");
        if (engDept == null)
        {
            engDept = Department.Create("Engineering");
            await _dbContext.Departments.AddAsync(engDept);
            await _dbContext.SaveChangesAsync();
        }

        var adminDesig = await _dbContext.Designations.FirstOrDefaultAsync(d => d.Title == "System Administrator" && d.DepartmentId == adminDept.DepartmentId);
        if (adminDesig == null)
        {
            adminDesig = Designation.Create("System Administrator", adminDept.DepartmentId);
            await _dbContext.Designations.AddAsync(adminDesig);
            await _dbContext.SaveChangesAsync();
        }

        var hrDesig = await _dbContext.Designations.FirstOrDefaultAsync(d => d.Title == "HR Manager" && d.DepartmentId == hrDept.DepartmentId);
        if (hrDesig == null)
        {
            hrDesig = Designation.Create("HR Manager", hrDept.DepartmentId);
            await _dbContext.Designations.AddAsync(hrDesig);
            await _dbContext.SaveChangesAsync();
        }

        var engDesig = await _dbContext.Designations.FirstOrDefaultAsync(d => d.Title == "Software Engineer" && d.DepartmentId == engDept.DepartmentId);
        if (engDesig == null)
        {
            engDesig = Designation.Create("Software Engineer", engDept.DepartmentId);
            await _dbContext.Designations.AddAsync(engDesig);
            await _dbContext.SaveChangesAsync();
        }

        var adminGrade = await _dbContext.Grades.FirstOrDefaultAsync(g => g.DesignationId == adminDesig.DesignationId && g.Level == "Executive");
        if (adminGrade == null)
        {
            adminGrade = Grade.Create(adminDesig.DesignationId, "Executive", "EX-01", 12000m);
            await _dbContext.Grades.AddAsync(adminGrade);
            await _dbContext.SaveChangesAsync();
        }

        var hrGrade = await _dbContext.Grades.FirstOrDefaultAsync(g => g.DesignationId == hrDesig.DesignationId && g.Level == "Management");
        if (hrGrade == null)
        {
            hrGrade = Grade.Create(hrDesig.DesignationId, "Management", "HR-01", 8500m);
            await _dbContext.Grades.AddAsync(hrGrade);
            await _dbContext.SaveChangesAsync();
        }

        var engGrade = await _dbContext.Grades.FirstOrDefaultAsync(g => g.DesignationId == engDesig.DesignationId && g.Level == "Senior");
        if (engGrade == null)
        {
            engGrade = Grade.Create(engDesig.DesignationId, "Senior", "ENG-01", 7500m);
            await _dbContext.Grades.AddAsync(engGrade);
            await _dbContext.SaveChangesAsync();
        }

        // 2. Seed Employees if none exist
        if (!await _dbContext.Employees.AnyAsync())
        {
            var adminEmp = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP-0001",
                FirstName = "Admin",
                LastName = "User",
                Email = _configuration["Seed:AdminEmail"] ?? "admin@hrms.local",
                Phone = "+1 555-0100",
                JoinDate = new DateOnly(2023, 1, 1),
                EmploymentType = EmploymentType.Permanent,
                DepartmentId = adminDept.DepartmentId,
                DesignationId = adminDesig.DesignationId,
                GradeId = adminGrade.GradeId,
                Status = EmployeeStatus.Active
            };

            var hrEmp = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP-0002",
                FirstName = "HR",
                LastName = "Manager",
                Email = _configuration["Seed:HrEmail"] ?? "hr@hrms.local",
                Phone = "+1 555-0101",
                JoinDate = new DateOnly(2023, 1, 1),
                EmploymentType = EmploymentType.Permanent,
                DepartmentId = hrDept.DepartmentId,
                DesignationId = hrDesig.DesignationId,
                GradeId = hrGrade.GradeId,
                Status = EmployeeStatus.Active
            };

            var regularEmp = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP-0003",
                FirstName = "John",
                LastName = "Doe",
                Email = _configuration["Seed:EmployeeEmail"] ?? "employee@hrms.local",
                Phone = "+1 555-0102",
                JoinDate = new DateOnly(2023, 2, 1),
                EmploymentType = EmploymentType.Permanent,
                DepartmentId = engDept.DepartmentId,
                DesignationId = engDesig.DesignationId,
                GradeId = engGrade.GradeId,
                Status = EmployeeStatus.Active
            };

            await _dbContext.Employees.AddRangeAsync(adminEmp, hrEmp, regularEmp);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Initial organizational data and employees seeded successfully.");
        }

        // 3. Link Admin, HR, and Employee users to their employee records
        var adminEmail = _configuration["Seed:AdminEmail"] ?? "admin@hrms.local";
        var hrEmail = _configuration["Seed:HrEmail"] ?? "hr@hrms.local";
        var empEmail = _configuration["Seed:EmployeeEmail"] ?? "employee@hrms.local";

        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        if (adminUser != null && !adminUser.EmployeeId.HasValue)
        {
            var emp = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Email == adminEmail);
            if (emp != null)
            {
                adminUser.EmployeeId = emp.EmployeeId;
                await _userManager.UpdateAsync(adminUser);
                _logger.LogInformation("Linked Admin user {Email} to employee {Code}", adminEmail, emp.EmployeeCode);
            }
        }

        var hrUser = await _userManager.FindByEmailAsync(hrEmail);
        if (hrUser != null && !hrUser.EmployeeId.HasValue)
        {
            var emp = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Email == hrEmail);
            if (emp != null)
            {
                hrUser.EmployeeId = emp.EmployeeId;
                await _userManager.UpdateAsync(hrUser);
                _logger.LogInformation("Linked HR user {Email} to employee {Code}", hrEmail, emp.EmployeeCode);
            }
        }

        var empUser = await _userManager.FindByEmailAsync(empEmail);
        if (empUser != null && !empUser.EmployeeId.HasValue)
        {
            var emp = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Email == empEmail);
            if (emp != null)
            {
                empUser.EmployeeId = emp.EmployeeId;
                await _userManager.UpdateAsync(empUser);
                _logger.LogInformation("Linked Employee user {Email} to employee {Code}", empEmail, emp.EmployeeCode);
            }
        }
    }

    private async Task SeedSettingsAsync()
    {
        var defaultSettings = new[]
        {
            (Key: SettingKeys.CompanyName, Value: "Company Name", Description: "Company name"),
            (Key: SettingKeys.CompanyTimezone, Value: "Asia/Dhaka", Description: "Time zone for WorkDate"),
            (Key: SettingKeys.AttendanceHalfDayHours, Value: "4", Description: "Fewer worked hours count as half day"),
            (Key: SettingKeys.AttendanceHolidayPriority, Value: "Holiday", Description: "Status when a holiday falls on a weekly off day (Holiday or Weekend)"),
            (Key: SettingKeys.LeaveYearStart, Value: "01-01", Description: "Leave year start (MM-dd)"),
            (Key: SettingKeys.PayrollPerDayDivisor, Value: "30", Description: "Divisor for per-day rate"),
            (Key: SettingKeys.EmployeeCodeFormat, Value: "EMP-{0:0000}", Description: "Employee code format")
        };

        var existingKeys = (await _dbContext.Settings
            .Select(s => s.SettingKey)
            .ToListAsync())
            .ToHashSet();

        var toAdd = new List<Setting>();
        foreach (var (key, value, description) in defaultSettings)
        {
            if (!existingKeys.Contains(key))
            {
                toAdd.Add(new Setting
                {
                    SettingId = Guid.NewGuid(),
                    SettingKey = key,
                    ValueText = value,
                    Description = description,
                    UpdatedAtUtc = DateTime.UtcNow,
                    UpdatedByUserId = null
                });
            }
        }

        if (toAdd.Count > 0)
        {
            await _dbContext.Settings.AddRangeAsync(toAdd);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} missing system settings.", toAdd.Count);
        }
    }

    private async Task SeedWorkingDaysAsync()
    {
        if (await _dbContext.CompanyWorkingDays.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var defaultDays = new (byte DayOfWeek, bool IsWorkingDay)[]
        {
            (0, true),  // Sunday - Working
            (1, true),  // Monday - Working
            (2, true),  // Tuesday - Working
            (3, true),  // Wednesday - Working
            (4, true),  // Thursday - Working
            (5, false), // Friday - Weekend off
            (6, false)  // Saturday - Weekend off
        };

        var workingDays = defaultDays.Select(d => new CompanyWorkingDay
        {
            CompanyWorkingDayId = Guid.NewGuid(),
            DayOfWeek = d.DayOfWeek,
            IsWorkingDay = d.IsWorkingDay,
            UpdatedAtUtc = now
        }).ToList();

        await _dbContext.CompanyWorkingDays.AddRangeAsync(workingDays);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Seeded 7 default CompanyWorkingDays (Friday and Saturday weekly off).");
    }
}