namespace AttendanceManagementSystem.ViewModels
{
    public class AdminMonthlySummaryViewModel
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

        public List<DepartmentSelectViewModel> Departments
        {
            get;
            set;
        } = new();

        // =====================================
        // 会社カレンダー
        // =====================================

        public bool IsCompanyCalendarGenerated
        {
            get;
            set;
        }

        public int CompanyWorkingDayCount
        {
            get;
            set;
        }

        public int CompanyHolidayCount
        {
            get;
            set;
        }

        // =====================================
        // 全社員集計
        // =====================================

        public int TotalEmployeeCount
        {
            get;
            set;
        }

        // 実際に出勤打刻した延べ日数
        public int TotalAttendanceDays
        {
            get;
            set;
        }

        // 承認済み有給の延べ取得日数
        public double TotalPaidLeaveDays
        {
            get;
            set;
        }

        // 管理者が欠勤として確定した延べ日数
        public int TotalAbsenceDays
        {
            get;
            set;
        }

        // 所定労働日だが、
        // 出勤・有給・欠勤確定のいずれもない延べ日数
        public double TotalMissingStampDays
        {
            get;
            set;
        }

        public int TotalWorkMinutes
        {
            get;
            set;
        }

        public int TotalOvertimeMinutes
        {
            get;
            set;
        }

        public int TotalLateMinutes
        {
            get;
            set;
        }

        public List<AdminMonthlySummaryItemViewModel> Items
        {
            get;
            set;
        } = new();
    }

    public class AdminMonthlySummaryItemViewModel
    {
        public int EmployeeId
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

        // =====================================
        // 社員別日数集計
        // =====================================

        public int ScheduledWorkDays
        {
            get;
            set;
        }

        public int HolidayDays
        {
            get;
            set;
        }

        // 実際に出勤打刻した日数
        public int WorkDays
        {
            get;
            set;
        }

        // 承認済み有給取得日数
        public double PaidLeaveDays
        {
            get;
            set;
        }

        // 管理者が欠勤として確定した日数
        public int AbsenceDays
        {
            get;
            set;
        }

        // 所定労働日だが、
        // 出勤・有給・欠勤確定のいずれもない日数
        public double MissingStampDays
        {
            get;
            set;
        }

        // =====================================
        // 社員別時間集計
        // =====================================

        public int TotalWorkMinutes
        {
            get;
            set;
        }

        public int TotalLateMinutes
        {
            get;
            set;
        }

        public int TotalOvertimeMinutes
        {
            get;
            set;
        }
    }
}