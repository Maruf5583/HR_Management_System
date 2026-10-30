using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    LeaveTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    DaysPerYear = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    RequiresDocument = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValueSql: "'Active'"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.LeaveTypeId);
                    table.CheckConstraint("CK_LeaveTypes_Days", "[DaysPerYear] >= 0");
                    table.CheckConstraint("CK_LeaveTypes_Status", "[Status] IN ('Active','Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                columns: table => new
                {
                    LeaveBalanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Entitled = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    Used = table.Column<decimal>(type: "decimal(4,1)", nullable: false, defaultValue: 0m),
                    Remaining = table.Column<decimal>(type: "decimal(4,1)", nullable: false, computedColumnSql: "[Entitled] - [Used]", stored: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.LeaveBalanceId);
                    table.CheckConstraint("CK_LeaveBalances_Values", "[Entitled] >= 0 AND [Used] >= 0 AND [Used] <= [Entitled]");
                    table.ForeignKey(
                        name: "FK_LeaveBalances_Employees",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_LeaveBalances_LeaveTypes",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "LeaveTypeId");
                });

            migrationBuilder.CreateTable(
                name: "LeaveRecords",
                columns: table => new
                {
                    LeaveRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsHalfDay = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TotalDays = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AttachmentKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValueSql: "'Approved'"),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRecords", x => x.LeaveRecordId);
                    table.CheckConstraint("CK_LeaveRecords_Days", "[TotalDays] > 0");
                    table.CheckConstraint("CK_LeaveRecords_Half", "[IsHalfDay] = 0 OR ([StartDate] = [EndDate] AND [TotalDays] = 0.5)");
                    table.CheckConstraint("CK_LeaveRecords_Range", "[EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_LeaveRecords_Reviewer", "([Status] NOT IN ('Approved','Rejected')) OR ([ReviewedByUserId] IS NOT NULL AND [ReviewedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_LeaveRecords_Status", "[Status] IN ('Pending','Approved','Rejected','Cancelled')");
                    table.ForeignKey(
                        name: "FK_LeaveRecords_Employees",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_LeaveRecords_LeaveTypes",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "LeaveTypeId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_LeaveType",
                table: "LeaveBalances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_LeaveBalances_Emp_Type_Year",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "LeaveTypeId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRecords_Emp_Start",
                table: "LeaveRecords",
                columns: new[] { "EmployeeId", "StartDate" })
                .Annotation("SqlServer:Include", new[] { "EndDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRecords_LeaveType",
                table: "LeaveRecords",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_LeaveTypes_Code",
                table: "LeaveTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LeaveTypes_Name",
                table: "LeaveTypes",
                column: "Name",
                unique: true);
        
            // One-time data step: insert leave.apply and leave.approve for Admin and HR roles, and leave.apply for Employee role
            migrationBuilder.Sql(@"
                -- Admin role
                IF EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE [Name] = 'Admin')
                BEGIN
                    DECLARE @AdminRoleId uniqueidentifier = (SELECT TOP 1 [Id] FROM dbo.AspNetRoles WHERE [Name] = 'Admin');
                    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoleClaims WHERE [RoleId] = @AdminRoleId AND [ClaimType] = 'permission' AND [ClaimValue] = 'leave.apply')
                        INSERT INTO dbo.AspNetRoleClaims ([RoleId], [ClaimType], [ClaimValue]) VALUES (@AdminRoleId, 'permission', 'leave.apply');
                    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoleClaims WHERE [RoleId] = @AdminRoleId AND [ClaimType] = 'permission' AND [ClaimValue] = 'leave.approve')
                        INSERT INTO dbo.AspNetRoleClaims ([RoleId], [ClaimType], [ClaimValue]) VALUES (@AdminRoleId, 'permission', 'leave.approve');
                END

                -- HR role
                IF EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE [Name] = 'HR')
                BEGIN
                    DECLARE @HrRoleId uniqueidentifier = (SELECT TOP 1 [Id] FROM dbo.AspNetRoles WHERE [Name] = 'HR');
                    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoleClaims WHERE [RoleId] = @HrRoleId AND [ClaimType] = 'permission' AND [ClaimValue] = 'leave.apply')
                        INSERT INTO dbo.AspNetRoleClaims ([RoleId], [ClaimType], [ClaimValue]) VALUES (@HrRoleId, 'permission', 'leave.apply');
                    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoleClaims WHERE [RoleId] = @HrRoleId AND [ClaimType] = 'permission' AND [ClaimValue] = 'leave.approve')
                        INSERT INTO dbo.AspNetRoleClaims ([RoleId], [ClaimType], [ClaimValue]) VALUES (@HrRoleId, 'permission', 'leave.approve');
                END

                -- Employee role
                IF EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE [Name] = 'Employee')
                BEGIN
                    DECLARE @EmployeeRoleId uniqueidentifier = (SELECT TOP 1 [Id] FROM dbo.AspNetRoles WHERE [Name] = 'Employee');
                    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoleClaims WHERE [RoleId] = @EmployeeRoleId AND [ClaimType] = 'permission' AND [ClaimValue] = 'leave.apply')
                        INSERT INTO dbo.AspNetRoleClaims ([RoleId], [ClaimType], [ClaimValue]) VALUES (@EmployeeRoleId, 'permission', 'leave.apply');
                END
            ");
        }

        /// <inheritdoc />
                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM dbo.AspNetRoleClaims 
                WHERE [ClaimType] = 'permission' AND [ClaimValue] IN ('leave.apply', 'leave.approve');
            ");
            migrationBuilder.DropTable(
                name: "LeaveBalances");

            migrationBuilder.DropTable(
                name: "LeaveRecords");

            migrationBuilder.DropTable(
                name: "LeaveTypes");
        }
    }
}
