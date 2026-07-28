using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyClosings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyClosings",
                columns: table => new
                {
                    MonthlyClosingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetYear = table.Column<int>(type: "int", nullable: false),
                    TargetMonth = table.Column<int>(type: "int", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ClosingComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReopenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReopenedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ReopenComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyClosings", x => x.MonthlyClosingId);
                    table.ForeignKey(
                        name: "FK_MonthlyClosings_Employees_ClosedByEmployeeId",
                        column: x => x.ClosedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_MonthlyClosings_Employees_ReopenedByEmployeeId",
                        column: x => x.ReopenedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosings_ClosedByEmployeeId",
                table: "MonthlyClosings",
                column: "ClosedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosings_IsClosed",
                table: "MonthlyClosings",
                column: "IsClosed");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosings_ReopenedByEmployeeId",
                table: "MonthlyClosings",
                column: "ReopenedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosings_TargetYear_TargetMonth",
                table: "MonthlyClosings",
                columns: new[] { "TargetYear", "TargetMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyClosings");
        }
    }
}
