using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceDays",
                columns: table => new
                {
                    AttendanceDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ShiftVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckInUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CheckOutUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LateMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    WorkedMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDays", x => x.AttendanceDayId);
                    table.CheckConstraint("CK_AttendanceDays_InOut", "[CheckOutUtc] IS NULL OR [CheckInUtc] IS NULL OR [CheckOutUtc] >= [CheckInUtc]");
                    table.CheckConstraint("CK_AttendanceDays_Minutes", "[LateMinutes] >= 0 AND [WorkedMinutes] >= 0");
                    table.CheckConstraint("CK_AttendanceDays_Status", "[Status] IN ('Present','Late','Absent','HalfDay','OnLeave','Holiday','Weekend')");
                    table.ForeignKey(
                        name: "FK_AttendanceDays_Employees",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_AttendanceDays_ShiftVersions",
                        column: x => x.ShiftVersionId,
                        principalTable: "ShiftVersions",
                        principalColumn: "ShiftVersionId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDays_Date_Status",
                table: "AttendanceDays",
                columns: new[] { "WorkDate", "Status" })
                .Annotation("SqlServer:Include", new[] { "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDays_ShiftVersion",
                table: "AttendanceDays",
                column: "ShiftVersionId",
                filter: "[ShiftVersionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_AttendanceDays_Emp_Date",
                table: "AttendanceDays",
                columns: new[] { "EmployeeId", "WorkDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceDays");
        }
    }
}
