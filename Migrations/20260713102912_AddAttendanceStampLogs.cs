using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceStampLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceStampLogs",
                columns: table => new
                {
                    AttendanceStampLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StampType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StampedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    AccuracyMeters = table.Column<double>(type: "float", nullable: true),
                    GpsStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceStampLogs", x => x.AttendanceStampLogId);
                    table.ForeignKey(
                        name: "FK_AttendanceStampLogs_Attendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalTable: "Attendances",
                        principalColumn: "AttendanceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceStampLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStampLogs_AttendanceId",
                table: "AttendanceStampLogs",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStampLogs_CreatedAt",
                table: "AttendanceStampLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStampLogs_EmployeeId",
                table: "AttendanceStampLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStampLogs_EmployeeId_StampedAt",
                table: "AttendanceStampLogs",
                columns: new[] { "EmployeeId", "StampedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStampLogs_StampedAt",
                table: "AttendanceStampLogs",
                column: "StampedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStampLogs_StampType",
                table: "AttendanceStampLogs",
                column: "StampType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceStampLogs");
        }
    }
}
