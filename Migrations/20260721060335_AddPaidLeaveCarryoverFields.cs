using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidLeaveCarryoverFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CarriedOverDays",
                table: "PaidLeaveBalances",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentGrantDate",
                table: "PaidLeaveBalances",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentGrantExpiryDate",
                table: "PaidLeaveBalances",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentGrantedDays",
                table: "PaidLeaveBalances",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ExpiredDays",
                table: "PaidLeaveBalances",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCalculatedAt",
                table: "PaidLeaveBalances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaidLeaveBalances_CurrentGrantDate",
                table: "PaidLeaveBalances",
                column: "CurrentGrantDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaidLeaveBalances_CurrentGrantExpiryDate",
                table: "PaidLeaveBalances",
                column: "CurrentGrantExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaidLeaveBalances_EmployeeId_Year",
                table: "PaidLeaveBalances",
                columns: new[] { "EmployeeId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaidLeaveBalances_Year",
                table: "PaidLeaveBalances",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaidLeaveBalances_CurrentGrantDate",
                table: "PaidLeaveBalances");

            migrationBuilder.DropIndex(
                name: "IX_PaidLeaveBalances_CurrentGrantExpiryDate",
                table: "PaidLeaveBalances");

            migrationBuilder.DropIndex(
                name: "IX_PaidLeaveBalances_EmployeeId_Year",
                table: "PaidLeaveBalances");

            migrationBuilder.DropIndex(
                name: "IX_PaidLeaveBalances_Year",
                table: "PaidLeaveBalances");

            migrationBuilder.DropColumn(
                name: "CarriedOverDays",
                table: "PaidLeaveBalances");

            migrationBuilder.DropColumn(
                name: "CurrentGrantDate",
                table: "PaidLeaveBalances");

            migrationBuilder.DropColumn(
                name: "CurrentGrantExpiryDate",
                table: "PaidLeaveBalances");

            migrationBuilder.DropColumn(
                name: "CurrentGrantedDays",
                table: "PaidLeaveBalances");

            migrationBuilder.DropColumn(
                name: "ExpiredDays",
                table: "PaidLeaveBalances");

            migrationBuilder.DropColumn(
                name: "LastCalculatedAt",
                table: "PaidLeaveBalances");
        }
    }
}
