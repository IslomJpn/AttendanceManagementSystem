using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId
        {
            get;
            set;
        }

        [Required]
        public int EmployeeId
        {
            get;
            set;
        }

        public Employee? Employee
        {
            get;
            set;
        }

        [Required]
        public DateTime AttendanceDate
        {
            get;
            set;
        }

        public TimeSpan? ClockInTime
        {
            get;
            set;
        }

        public TimeSpan? ClockOutTime
        {
            get;
            set;
        }

        // 実際に勤務時間と重なった休憩時間
        public int BreakMinutes
        {
            get;
            set;
        } = 0;

        // 実働時間
        public int WorkMinutes
        {
            get;
            set;
        } = 0;

        // 遅刻時間
        public int LateMinutes
        {
            get;
            set;
        } = 0;

        // 残業時間
        public int OvertimeMinutes
        {
            get;
            set;
        } = 0;

        // 現在の勤怠状態
        // 未出勤・出勤中・遅刻・退勤済み・欠勤
        [StringLength(20)]
        public string Status
        {
            get;
            set;
        } = "未出勤";

        // =====================================
        // 欠勤確定情報
        // =====================================

        // 管理者が欠勤として確定したか
        public bool IsAbsent
        {
            get;
            set;
        } = false;

        // 欠勤理由
        [StringLength(
            300,
            ErrorMessage =
                "欠勤理由は300文字以内で入力してください。"
        )]
        public string? AbsenceReason
        {
            get;
            set;
        }

        // 欠勤として確定した日時
        public DateTime? AbsenceConfirmedAt
        {
            get;
            set;
        }

        // 欠勤を確定した管理者の社員ID
        public int? AbsenceConfirmedBy
        {
            get;
            set;
        }

        // =====================================
        // 勤務条件スナップショット
        // 打刻日の勤務条件を保存する
        // =====================================

        // 打刻日時点の所定出勤時刻
        public TimeSpan ScheduledStartTimeSnapshot
        {
            get;
            set;
        } = new TimeSpan(
            9,
            0,
            0
        );

        // 打刻日時点の所定退勤時刻
        public TimeSpan ScheduledEndTimeSnapshot
        {
            get;
            set;
        } = new TimeSpan(
            18,
            0,
            0
        );

        // 打刻日時点の所定労働時間（分）
        [Range(
            1,
            1440,
            ErrorMessage =
                "所定労働時間は1分以上1440分以内で設定してください。"
        )]
        public int ScheduledWorkMinutesSnapshot
        {
            get;
            set;
        } = 480;

        // 打刻日時点の昼休憩開始時刻
        public TimeSpan LunchBreakStartTimeSnapshot
        {
            get;
            set;
        } = new TimeSpan(
            12,
            0,
            0
        );

        // 打刻日時点の昼休憩終了時刻
        public TimeSpan LunchBreakEndTimeSnapshot
        {
            get;
            set;
        } = new TimeSpan(
            13,
            0,
            0
        );

        // 打刻日時点の小休憩1
        public TimeSpan? SmallBreak1StartTimeSnapshot
        {
            get;
            set;
        }

        public TimeSpan? SmallBreak1EndTimeSnapshot
        {
            get;
            set;
        }

        // 打刻日時点の小休憩2
        public TimeSpan? SmallBreak2StartTimeSnapshot
        {
            get;
            set;
        }

        public TimeSpan? SmallBreak2EndTimeSnapshot
        {
            get;
            set;
        }

        public DateTime CreatedAt
        {
            get;
            set;
        } = DateTime.Now;

        public DateTime UpdatedAt
        {
            get;
            set;
        } = DateTime.Now;
    }
}