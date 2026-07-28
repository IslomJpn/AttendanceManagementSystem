namespace AttendanceManagementSystem.ViewModels
{
    public class AttendanceHistoryViewModel
    {
        public string YearMonth
        {
            get;
            set;
        } = DateTime.Today.ToString("yyyy-MM");

        public List<AttendanceHistoryItemViewModel> Items
        {
            get;
            set;
        } = new();
    }

    public class AttendanceHistoryItemViewModel
    {
        public int? AttendanceId
        {
            get;
            set;
        }

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

        public int BreakMinutes
        {
            get;
            set;
        }

        public int WorkMinutes
        {
            get;
            set;
        }

        public int LateMinutes
        {
            get;
            set;
        }

        public int OvertimeMinutes
        {
            get;
            set;
        }

        public string Status
        {
            get;
            set;
        } = string.Empty;

        public bool IsCompanyWorkingDay
        {
            get;
            set;
        }

        public bool HasAttendanceRecord
        {
            get;
            set;
        }

        public bool HasApprovedPaidLeave
        {
            get;
            set;
        }

        public bool IsAbsent
        {
            get;
            set;
        }

        public string AbsenceReason
        {
            get;
            set;
        } = string.Empty;

        public DateTime? AbsenceConfirmedAt
        {
            get;
            set;
        }

        public int? AbsenceConfirmedBy
        {
            get;
            set;
        }

        public string AbsenceConfirmedByName
        {
            get;
            set;
        } = string.Empty;
    }
}