namespace AttendanceManagementSystem.ViewModels
{
    public class AdminAttendanceListViewModel
    {
        public string YearMonth
        {
            get;
            set;
        } = DateTime.Today.ToString("yyyy-MM");

        public int? DepartmentId
        {
            get;
            set;
        }

        public string? Keyword
        {
            get;
            set;
        }

        public List<DepartmentSelectViewModel> Departments
        {
            get;
            set;
        } = new();

        public List<AdminAttendanceListItemViewModel> Attendances
        {
            get;
            set;
        } = new();
    }

    public class AdminAttendanceListItemViewModel
    {
        public int? AttendanceId
        {
            get;
            set;
        }

        public int EmployeeId
        {
            get;
            set;
        }

        public DateTime AttendanceDate
        {
            get;
            set;
        }

        public string EmployeeName
        {
            get;
            set;
        } = string.Empty;

        public string DepartmentName
        {
            get;
            set;
        } = string.Empty;

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
        } = "未打刻";

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

        // 対象日に申請中の勤怠修正申請があるか
        public bool HasPendingCorrectionRequest
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

        public bool CanConfirmAbsence =>
            IsCompanyWorkingDay &&
            !HasApprovedPaidLeave &&
            !HasPendingCorrectionRequest &&
            !ClockInTime.HasValue &&
            !IsAbsent;

        public bool CanCancelAbsence =>
            IsAbsent;
    }
}