using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidLeaveGrantHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaidLeaveGrantHistories",
                columns: table => new
                {
                    PaidLeaveGrantHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    GrantDate = table.Column<DateTime>(type: "date", nullable: false),
                    AttendanceCheckStartDate = table.Column<DateTime>(type: "date", nullable: false),
                    AttendanceCheckEndDate = table.Column<DateTime>(type: "date", nullable: false),
                    TotalWorkDays = table.Column<int>(type: "int", nullable: false),
                    AttendedDays = table.Column<int>(type: "int", nullable: false),
                    AttendanceRate = table.Column<double>(type: "float", nullable: false),
                    IsAttendanceRateEnough = table.Column<bool>(type: "bit", nullable: false),
                    GrantStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GrantedDays = table.Column<double>(type: "float", nullable: false),
                    UsedDays = table.Column<double>(type: "float", nullable: false),
                    RemainingDays = table.Column<double>(type: "float", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaidLeaveGrantHistories", x => x.PaidLeaveGrantHistoryId);
                    table.ForeignKey(
                        name: "FK_PaidLeaveGrantHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaidLeaveGrantHistories_EmployeeId",
                table: "PaidLeaveGrantHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaidLeaveGrantHistories_EmployeeId_GrantDate",
                table: "PaidLeaveGrantHistories",
                columns: new[] { "EmployeeId", "GrantDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaidLeaveGrantHistories_GrantDate",
                table: "PaidLeaveGrantHistories",
                column: "GrantDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaidLeaveGrantHistories_GrantStatus",
                table: "PaidLeaveGrantHistories",
                column: "GrantStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaidLeaveGrantHistories");
        }
    }
}
