namespace AttendanceManagementSystem.ViewModels
{
    public class AttendanceIndexViewModel
    {
        public string EmployeeName { get; set; }
            = string.Empty;

        public string DepartmentName { get; set; }
            = string.Empty;

        public DateTime Today { get; set; }
            = DateTime.Today;

        public DateTime CurrentTime { get; set; }
            = DateTime.Now;

        public TimeSpan? ClockInTime { get; set; }

        public TimeSpan? ClockOutTime { get; set; }

        // 実際の勤務時間と重なった休憩時間
        public int BreakMinutes { get; set; }
            = 0;

        public int WorkMinutes { get; set; }

        public int LateMinutes { get; set; }

        public int OvertimeMinutes { get; set; }

        public double RemainingPaidLeaveDays { get; set; }

        public string Status { get; set; }
            = "未出勤";

        public string? Message { get; set; }

        // =====================================
        // 本日の会社カレンダー
        // =====================================

        // true  = 所定労働日
        // false = 休日
        public bool IsCompanyWorkingDay { get; set; }
            = true;

        // 出勤日、会社休日、法定休日、祝日など
        public string TodayDayType { get; set; }
            = "出勤日";

        // 祝日名・会社休日名
        // 例：海の日、創立記念日、夏季休暇
        public string? TodayHolidayName { get; set; }

        // =====================================
        // 本日の勤務条件
        // =====================================

        // 所定出勤時刻
        public TimeSpan ScheduledStartTime { get; set; }
            = new TimeSpan(9, 0, 0);

        // 所定退勤時刻
        public TimeSpan ScheduledEndTime { get; set; }
            = new TimeSpan(18, 0, 0);

        // 所定労働時間（分）
        public int ScheduledWorkMinutes { get; set; }
            = 480;

        // 昼休憩
        public TimeSpan LunchBreakStartTime { get; set; }
            = new TimeSpan(12, 0, 0);

        public TimeSpan LunchBreakEndTime { get; set; }
            = new TimeSpan(13, 0, 0);

        // 小休憩1
        public TimeSpan? SmallBreak1StartTime { get; set; }

        public TimeSpan? SmallBreak1EndTime { get; set; }

        // 小休憩2
        public TimeSpan? SmallBreak2StartTime { get; set; }

        public TimeSpan? SmallBreak2EndTime { get; set; }

        // =====================================
        // GPS情報
        // =====================================

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public double? AccuracyMeters { get; set; }

        public string GpsStatus { get; set; }
            = "未取得";

        // =====================================
        // 打刻忘れアラート
        // =====================================

        public bool IsClockInMissingAlert { get; set; }

        public bool IsClockOutMissingAlert { get; set; }

        public string AttendanceAlertMessage { get; set; }
            = string.Empty;
    }
}
