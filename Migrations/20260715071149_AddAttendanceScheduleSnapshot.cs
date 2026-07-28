using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceScheduleSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
            // 昼休憩終了時刻：13:00
            migrationBuilder.AddColumn<TimeSpan>(
                name: "LunchBreakEndTimeSnapshot",
                table: "Attendances",
                type: "time",
                nullable: false,
                defaultValue:
                    new TimeSpan(0, 13, 0, 0, 0));

            // 昼休憩開始時刻：12:00
            migrationBuilder.AddColumn<TimeSpan>(
                name: "LunchBreakStartTimeSnapshot",
                table: "Attendances",
                type: "time",
                nullable: false,
                defaultValue:
                    new TimeSpan(0, 12, 0, 0, 0));

            // 所定退勤時刻：18:00
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduledEndTimeSnapshot",
                table: "Attendances",
                type: "time",
                nullable: false,
                defaultValue:
                    new TimeSpan(0, 18, 0, 0, 0));

            // 所定出勤時刻：09:00
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduledStartTimeSnapshot",
                table: "Attendances",
                type: "time",
                nullable: false,
                defaultValue:
                    new TimeSpan(0, 9, 0, 0, 0));

            // 所定労働時間：480分
            migrationBuilder.AddColumn<int>(
                name: "ScheduledWorkMinutesSnapshot",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 480);

            // 小休憩1終了時刻
            migrationBuilder.AddColumn<TimeSpan>(
                name: "SmallBreak1EndTimeSnapshot",
                table: "Attendances",
                type: "time",
                nullable: true);

            // 小休憩1開始時刻
            migrationBuilder.AddColumn<TimeSpan>(
                name: "SmallBreak1StartTimeSnapshot",
                table: "Attendances",
                type: "time",
                nullable: true);

            // 小休憩2終了時刻
            migrationBuilder.AddColumn<TimeSpan>(
                name: "SmallBreak2EndTimeSnapshot",
                table: "Attendances",
                type: "time",
                nullable: true);

            // 小休憩2開始時刻
            migrationBuilder.AddColumn<TimeSpan>(
                name: "SmallBreak2StartTimeSnapshot",
                table: "Attendances",
                type: "time",
                nullable: true);

            // 既存の勤怠データには、
            // 各社員の現在の勤務条件をコピーする
            migrationBuilder.Sql(
                """
                UPDATE attendances
                SET
                    ScheduledStartTimeSnapshot =
                        employees.ScheduledStartTime,

                    ScheduledEndTimeSnapshot =
                        employees.ScheduledEndTime,

                    ScheduledWorkMinutesSnapshot =
                        employees.ScheduledWorkMinutes,

                    LunchBreakStartTimeSnapshot =
                        employees.LunchBreakStartTime,

                    LunchBreakEndTimeSnapshot =
                        employees.LunchBreakEndTime,

                    SmallBreak1StartTimeSnapshot =
                        employees.SmallBreak1StartTime,

                    SmallBreak1EndTimeSnapshot =
                        employees.SmallBreak1EndTime,

                    SmallBreak2StartTimeSnapshot =
                        employees.SmallBreak2StartTime,

                    SmallBreak2EndTimeSnapshot =
                        employees.SmallBreak2EndTime

                FROM Attendances AS attendances
                INNER JOIN Employees AS employees
                    ON attendances.EmployeeId =
                       employees.EmployeeId;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LunchBreakEndTimeSnapshot",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "LunchBreakStartTimeSnapshot",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ScheduledEndTimeSnapshot",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ScheduledStartTimeSnapshot",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ScheduledWorkMinutesSnapshot",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "SmallBreak1EndTimeSnapshot",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "SmallBreak1StartTimeSnapshot",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "SmallBreak2EndTimeSnapshot",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "SmallBreak2StartTimeSnapshot",
                table: "Attendances");
        }
    }
}