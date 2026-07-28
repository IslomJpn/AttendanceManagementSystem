namespace AttendanceManagementSystem.ViewModels
{
    public class PaidLeaveBalanceViewModel
    {
        // =====================================
        // 現在の有給残高
        // =====================================

        public double GrantedDays
        {
            get;
            set;
        }

        public double UsedDays
        {
            get;
            set;
        }
        public double ReservedDays
        {
            get;
            set;
        }

        public double RemainingDays
        {
            get;
            set;
        }

        public double CurrentGrantedDays
        {
            get;
            set;
        }

        public double CarriedOverDays
        {
            get;
            set;
        }

        public double ExpiredDays
        {
            get;
            set;
        }

        public DateTime? CurrentGrantExpiryDate
        {
            get;
            set;
        }

        public DateTime? LastCalculatedAt
        {
            get;
            set;
        }

        public double RequiredDays
        {
            get;
            set;
        }

        // =====================================
        // 法定有給情報
        // =====================================

        public DateTime JoinDate
        {
            get;
            set;
        }

        public DateTime? CurrentGrantDate
        {
            get;
            set;
        }

        public DateTime NextGrantDate
        {
            get;
            set;
        }

        public DateTime? FiveDayDeadline
        {
            get;
            set;
        }

        public DateTime? LegalExpiryDate
        {
            get;
            set;
        }

        public int LegalGrantedDays
        {
            get;
            set;
        }

        public int TotalWorkDaysInCheckPeriod
        {
            get;
            set;
        }

        public int AttendedDaysInCheckPeriod
        {
            get;
            set;
        }

        public double AttendanceRate
        {
            get;
            set;
        }

        public bool IsAttendanceRateEnough
        {
            get;
            set;
        }

        public double UsedDaysAfterCurrentGrant
        {
            get;
            set;
        }

        public double RemainingFiveDayRequirement
        {
            get;
            set;
        }

        public bool IsFiveDayAlertTarget
        {
            get;
            set;
        }

        public string LegalMessage
        {
            get;
            set;
        } = string.Empty;

        // =====================================
        // 有給付与履歴
        // =====================================

        public List<PaidLeaveGrantHistoryItemViewModel>
            GrantHistories
        {
            get;
            set;
        } = new();

        // =====================================
        // 有給申請履歴
        // =====================================

        public List<PaidLeaveHistoryItemViewModel>
            Requests
        {
            get;
            set;
        } = new();
    }

    // =====================================
    // 有給付与履歴
    // =====================================

    public class PaidLeaveGrantHistoryItemViewModel
    {
        public int PaidLeaveGrantHistoryId
        {
            get;
            set;
        }

        public DateTime GrantDate
        {
            get;
            set;
        }

        public DateTime AttendanceCheckStartDate
        {
            get;
            set;
        }

        public DateTime AttendanceCheckEndDate
        {
            get;
            set;
        }

        public int TotalWorkDays
        {
            get;
            set;
        }

        public int AttendedDays
        {
            get;
            set;
        }

        public double AttendanceRate
        {
            get;
            set;
        }

        public bool IsAttendanceRateEnough
        {
            get;
            set;
        }

        public string GrantStatus
        {
            get;
            set;
        } = string.Empty;

        public double GrantedDays
        {
            get;
            set;
        }

        public double UsedDays
        {
            get;
            set;
        }

        public double RemainingDays
        {
            get;
            set;
        }

        public DateTime? ExpiryDate
        {
            get;
            set;
        }

        public string DecisionReason
        {
            get;
            set;
        } = string.Empty;

        public DateTime CreatedAt
        {
            get;
            set;
        }

        public DateTime UpdatedAt
        {
            get;
            set;
        }
    }

    // =====================================
    // 有給申請履歴
    // =====================================

    public class PaidLeaveHistoryItemViewModel
    {
        public int PaidLeaveRequestId
        {
            get;
            set;
        }

        public DateTime LeaveDate
        {
            get;
            set;
        }

        public double Days
        {
            get;
            set;
        }

        public string Reason
        {
            get;
            set;
        } = string.Empty;

        public string Status
        {
            get;
            set;
        } = string.Empty;

        public DateTime CreatedAt
        {
            get;
            set;
        }

        public DateTime? ApprovedAt
        {
            get;
            set;
        }

        public int? ApprovedBy
        {
            get;
            set;
        }

        public string ApprovedByName
        {
            get;
            set;
        } = string.Empty;

        public string AdminComment
        {
            get;
            set;
        } = string.Empty;
    }
}