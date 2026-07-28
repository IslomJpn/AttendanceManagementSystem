using AttendanceManagementSystem.Models;

namespace AttendanceManagementSystem.Services
{
    public class AttendanceCalculationService
    {
        // 社員の現在の勤務条件を勤怠データへ保存する
        public void ApplyScheduleSnapshot(
            Employee employee,
            Attendance attendance)
        {
            ArgumentNullException.ThrowIfNull(employee);
            ArgumentNullException.ThrowIfNull(attendance);

            attendance.ScheduledStartTimeSnapshot =
                employee.ScheduledStartTime;

            attendance.ScheduledEndTimeSnapshot =
                employee.ScheduledEndTime;

            attendance.ScheduledWorkMinutesSnapshot =
                employee.ScheduledWorkMinutes;

            attendance.LunchBreakStartTimeSnapshot =
                employee.LunchBreakStartTime;

            attendance.LunchBreakEndTimeSnapshot =
                employee.LunchBreakEndTime;

            attendance.SmallBreak1StartTimeSnapshot =
                employee.SmallBreak1StartTime;

            attendance.SmallBreak1EndTimeSnapshot =
                employee.SmallBreak1EndTime;

            attendance.SmallBreak2StartTimeSnapshot =
                employee.SmallBreak2StartTime;

            attendance.SmallBreak2EndTimeSnapshot =
                employee.SmallBreak2EndTime;
        }

        // =====================================
        // 勤怠スナップショットを使用する計算
        // =====================================

        // 遅刻・休憩・実働・残業をまとめて計算
        public AttendanceCalculationResult Calculate(
            Attendance attendance,
            TimeSpan clockInTime,
            TimeSpan clockOutTime)
        {
            ArgumentNullException.ThrowIfNull(attendance);

            return CalculateInternal(
                scheduledStartTime:
                    attendance.ScheduledStartTimeSnapshot,

                scheduledWorkMinutes:
                    attendance.ScheduledWorkMinutesSnapshot,

                lunchBreakStartTime:
                    attendance.LunchBreakStartTimeSnapshot,

                lunchBreakEndTime:
                    attendance.LunchBreakEndTimeSnapshot,

                smallBreak1StartTime:
                    attendance.SmallBreak1StartTimeSnapshot,

                smallBreak1EndTime:
                    attendance.SmallBreak1EndTimeSnapshot,

                smallBreak2StartTime:
                    attendance.SmallBreak2StartTimeSnapshot,

                smallBreak2EndTime:
                    attendance.SmallBreak2EndTimeSnapshot,

                clockInTime:
                    clockInTime,

                clockOutTime:
                    clockOutTime
            );
        }

        // 勤怠スナップショットを基準に遅刻時間を計算
        public int CalculateLateMinutes(
            Attendance attendance,
            TimeSpan clockInTime)
        {
            ArgumentNullException.ThrowIfNull(attendance);

            return CalculateLateMinutesInternal(
                attendance.ScheduledStartTimeSnapshot,
                clockInTime
            );
        }

        // 勤怠スナップショットを基準に休憩時間を計算
        public int CalculateBreakMinutes(
            Attendance attendance,
            TimeSpan clockInTime,
            TimeSpan clockOutTime)
        {
            ArgumentNullException.ThrowIfNull(attendance);

            return CalculateBreakMinutesInternal(
                clockInTime,
                clockOutTime,

                attendance.LunchBreakStartTimeSnapshot,
                attendance.LunchBreakEndTimeSnapshot,

                attendance.SmallBreak1StartTimeSnapshot,
                attendance.SmallBreak1EndTimeSnapshot,

                attendance.SmallBreak2StartTimeSnapshot,
                attendance.SmallBreak2EndTimeSnapshot
            );
        }

        // 勤怠スナップショットを基準に残業時間を計算
        public int CalculateOvertimeMinutes(
            Attendance attendance,
            int workMinutes)
        {
            ArgumentNullException.ThrowIfNull(attendance);

            return Math.Max(
                0,
                workMinutes -
                attendance.ScheduledWorkMinutesSnapshot
            );
        }

        // =====================================
        // 社員の現在の勤務条件を使用する計算
        // 新しい勤怠作成前や互換処理で使用する
        // =====================================

        public AttendanceCalculationResult Calculate(
            Employee employee,
            TimeSpan clockInTime,
            TimeSpan clockOutTime)
        {
            ArgumentNullException.ThrowIfNull(employee);

            return CalculateInternal(
                scheduledStartTime:
                    employee.ScheduledStartTime,

                scheduledWorkMinutes:
                    employee.ScheduledWorkMinutes,

                lunchBreakStartTime:
                    employee.LunchBreakStartTime,

                lunchBreakEndTime:
                    employee.LunchBreakEndTime,

                smallBreak1StartTime:
                    employee.SmallBreak1StartTime,

                smallBreak1EndTime:
                    employee.SmallBreak1EndTime,

                smallBreak2StartTime:
                    employee.SmallBreak2StartTime,

                smallBreak2EndTime:
                    employee.SmallBreak2EndTime,

                clockInTime:
                    clockInTime,

                clockOutTime:
                    clockOutTime
            );
        }

        public int CalculateLateMinutes(
            Employee employee,
            TimeSpan clockInTime)
        {
            ArgumentNullException.ThrowIfNull(employee);

            return CalculateLateMinutesInternal(
                employee.ScheduledStartTime,
                clockInTime
            );
        }

        public int CalculateBreakMinutes(
            Employee employee,
            TimeSpan clockInTime,
            TimeSpan clockOutTime)
        {
            ArgumentNullException.ThrowIfNull(employee);

            return CalculateBreakMinutesInternal(
                clockInTime,
                clockOutTime,

                employee.LunchBreakStartTime,
                employee.LunchBreakEndTime,

                employee.SmallBreak1StartTime,
                employee.SmallBreak1EndTime,

                employee.SmallBreak2StartTime,
                employee.SmallBreak2EndTime
            );
        }

        public int CalculateOvertimeMinutes(
            Employee employee,
            int workMinutes)
        {
            ArgumentNullException.ThrowIfNull(employee);

            return Math.Max(
                0,
                workMinutes -
                employee.ScheduledWorkMinutes
            );
        }

        // =====================================
        // 共通計算処理
        // =====================================

        private static AttendanceCalculationResult
            CalculateInternal(
                TimeSpan scheduledStartTime,
                int scheduledWorkMinutes,
                TimeSpan lunchBreakStartTime,
                TimeSpan lunchBreakEndTime,
                TimeSpan? smallBreak1StartTime,
                TimeSpan? smallBreak1EndTime,
                TimeSpan? smallBreak2StartTime,
                TimeSpan? smallBreak2EndTime,
                TimeSpan clockInTime,
                TimeSpan clockOutTime)
        {
            var lateMinutes =
                CalculateLateMinutesInternal(
                    scheduledStartTime,
                    clockInTime
                );

            if (clockOutTime <= clockInTime)
            {
                return new AttendanceCalculationResult
                {
                    BreakMinutes = 0,
                    WorkMinutes = 0,
                    LateMinutes = lateMinutes,
                    OvertimeMinutes = 0
                };
            }

            var totalMinutes =
                (int)(
                    clockOutTime -
                    clockInTime
                ).TotalMinutes;

            var breakMinutes =
                CalculateBreakMinutesInternal(
                    clockInTime,
                    clockOutTime,

                    lunchBreakStartTime,
                    lunchBreakEndTime,

                    smallBreak1StartTime,
                    smallBreak1EndTime,

                    smallBreak2StartTime,
                    smallBreak2EndTime
                );

            var workMinutes =
                Math.Max(
                    0,
                    totalMinutes -
                    breakMinutes
                );

            var overtimeMinutes =
                Math.Max(
                    0,
                    workMinutes -
                    scheduledWorkMinutes
                );

            return new AttendanceCalculationResult
            {
                BreakMinutes = breakMinutes,
                WorkMinutes = workMinutes,
                LateMinutes = lateMinutes,
                OvertimeMinutes = overtimeMinutes
            };
        }

        private static int CalculateLateMinutesInternal(
            TimeSpan scheduledStartTime,
            TimeSpan clockInTime)
        {
            if (clockInTime <= scheduledStartTime)
            {
                return 0;
            }

            return Math.Max(
                0,
                (int)(
                    clockInTime -
                    scheduledStartTime
                ).TotalMinutes
            );
        }

        private static int CalculateBreakMinutesInternal(
            TimeSpan clockInTime,
            TimeSpan clockOutTime,
            TimeSpan lunchBreakStartTime,
            TimeSpan lunchBreakEndTime,
            TimeSpan? smallBreak1StartTime,
            TimeSpan? smallBreak1EndTime,
            TimeSpan? smallBreak2StartTime,
            TimeSpan? smallBreak2EndTime)
        {
            if (clockOutTime <= clockInTime)
            {
                return 0;
            }

            var breakMinutes = 0;

            // 昼休憩
            breakMinutes +=
                CalculateOverlapMinutes(
                    clockInTime,
                    clockOutTime,
                    lunchBreakStartTime,
                    lunchBreakEndTime
                );

            // 小休憩1
            if (smallBreak1StartTime.HasValue &&
                smallBreak1EndTime.HasValue)
            {
                breakMinutes +=
                    CalculateOverlapMinutes(
                        clockInTime,
                        clockOutTime,
                        smallBreak1StartTime.Value,
                        smallBreak1EndTime.Value
                    );
            }

            // 小休憩2
            if (smallBreak2StartTime.HasValue &&
                smallBreak2EndTime.HasValue)
            {
                breakMinutes +=
                    CalculateOverlapMinutes(
                        clockInTime,
                        clockOutTime,
                        smallBreak2StartTime.Value,
                        smallBreak2EndTime.Value
                    );
            }

            return Math.Max(
                0,
                breakMinutes
            );
        }

        // 勤務時間と休憩時間が重なった部分のみ計算する
        private static int CalculateOverlapMinutes(
            TimeSpan workStartTime,
            TimeSpan workEndTime,
            TimeSpan breakStartTime,
            TimeSpan breakEndTime)
        {
            if (workEndTime <= workStartTime ||
                breakEndTime <= breakStartTime)
            {
                return 0;
            }

            var overlapStartTime =
                workStartTime > breakStartTime
                    ? workStartTime
                    : breakStartTime;

            var overlapEndTime =
                workEndTime < breakEndTime
                    ? workEndTime
                    : breakEndTime;

            if (overlapEndTime <= overlapStartTime)
            {
                return 0;
            }

            return Math.Max(
                0,
                (int)(
                    overlapEndTime -
                    overlapStartTime
                ).TotalMinutes
            );
        }
    }

    public class AttendanceCalculationResult
    {
        public int BreakMinutes { get; set; }

        public int WorkMinutes { get; set; }

        public int LateMinutes { get; set; }

        public int OvertimeMinutes { get; set; }
    }
}