using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCalendarDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyCalendarDays",
                columns: table => new
                {
                    CompanyCalendarDayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalendarDate = table.Column<DateTime>(type: "date", nullable: false),
                    DayType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: false),
                    HolidayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCalendarDays", x => x.CompanyCalendarDayId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCalendarDays_CalendarDate",
                table: "CompanyCalendarDays",
                column: "CalendarDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCalendarDays_CalendarDate_IsWorkingDay",
                table: "CompanyCalendarDays",
                columns: new[] { "CalendarDate", "IsWorkingDay" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCalendarDays_DayType",
                table: "CompanyCalendarDays",
                column: "DayType");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCalendarDays_IsWorkingDay",
                table: "CompanyCalendarDays",
                column: "IsWorkingDay");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyCalendarDays");
        }
    }
}
