using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeWorkSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
            // 昼休憩終了時刻：13:00
            migrationBuilder.AddColumn<TimeSpan>(
                name: "LunchBreakEndTime",
                table: "Employees",
                type: "time",
                nullable: false,
                defaultValue:
                    new TimeSpan(0, 13, 0, 0, 0));

            // 昼休憩開始時刻：12:00
            migrationBuilder.AddColumn<TimeSpan>(
                name: "LunchBreakStartTime",
                table: "Employees",
                type: "time",
                nullable: false,
                defaultValue:
                    new TimeSpan(0, 12, 0, 0, 0));

            // 所定退勤時刻：18:00
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduledEndTime",
                table: "Employees",
                type: "time",
                nullable: false,
                defaultValue:
                    new TimeSpan(0, 18, 0, 0, 0));

            // 所定出勤時刻：09:00
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduledStartTime",
                table: "Employees",
                type: "time",
                nullable: false,
                defaultValue:
                    new TimeSpan(0, 9, 0, 0, 0));

            // 1日の所定労働時間：480分（8時間）
            migrationBuilder.AddColumn<int>(
                name: "ScheduledWorkMinutes",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 480);

            // 小休憩1終了時刻：未設定
            migrationBuilder.AddColumn<TimeSpan>(
                name: "SmallBreak1EndTime",
                table: "Employees",
                type: "time",
                nullable: true);

            // 小休憩1開始時刻：未設定
            migrationBuilder.AddColumn<TimeSpan>(
                name: "SmallBreak1StartTime",
                table: "Employees",
                type: "time",
                nullable: true);

            // 小休憩2終了時刻：未設定
            migrationBuilder.AddColumn<TimeSpan>(
                name: "SmallBreak2EndTime",
                table: "Employees",
                type: "time",
                nullable: true);

            // 小休憩2開始時刻：未設定
            migrationBuilder.AddColumn<TimeSpan>(
                name: "SmallBreak2StartTime",
                table: "Employees",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LunchBreakEndTime",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LunchBreakStartTime",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ScheduledEndTime",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ScheduledStartTime",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ScheduledWorkMinutes",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SmallBreak1EndTime",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SmallBreak1StartTime",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SmallBreak2EndTime",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SmallBreak2StartTime",
                table: "Employees");
        }
    }
}