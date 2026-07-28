using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceAbsenceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AbsenceConfirmedAt",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AbsenceConfirmedBy",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbsenceReason",
                table: "Attendances",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAbsent",
                table: "Attendances",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsenceConfirmedAt",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "AbsenceConfirmedBy",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "AbsenceReason",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "IsAbsent",
                table: "Attendances");
        }
    }
}
