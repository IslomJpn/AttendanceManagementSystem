using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class PaidLeaveGrantHistory
    {
        public int PaidLeaveGrantHistoryId
        {
            get;
            set;
        }

        [Required]
        [Display(Name = "社員ID")]
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
        [DataType(DataType.Date)]
        [Display(Name = "付与日")]
        public DateTime GrantDate
        {
            get;
            set;
        }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "出勤率確認開始日")]
        public DateTime AttendanceCheckStartDate
        {
            get;
            set;
        }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "出勤率確認終了日")]
        public DateTime AttendanceCheckEndDate
        {
            get;
            set;
        }

        [Display(Name = "所定労働日数")]
        public int TotalWorkDays
        {
            get;
            set;
        }

        [Display(Name = "出勤扱い日数")]
        public int AttendedDays
        {
            get;
            set;
        }

        [Display(Name = "出勤率")]
        public double AttendanceRate
        {
            get;
            set;
        }

        [Display(Name = "80％条件")]
        public bool IsAttendanceRateEnough
        {
            get;
            set;
        }

        [Display(Name = "付与結果")]
        [StringLength(20)]
        public string GrantStatus
        {
            get;
            set;
        } = "付与";

        [Display(Name = "付与日数")]
        public double GrantedDays
        {
            get;
            set;
        }

        [Display(Name = "使用日数")]
        public double UsedDays
        {
            get;
            set;
        }

        [Display(Name = "残日数")]
        public double RemainingDays
        {
            get;
            set;
        }

        [DataType(DataType.Date)]
        [Display(Name = "有効期限")]
        public DateTime? ExpiryDate
        {
            get;
            set;
        }

        [StringLength(500)]
        [Display(Name = "判定理由")]
        public string? DecisionReason
        {
            get;
            set;
        }

        [Display(Name = "登録日時")]
        public DateTime CreatedAt
        {
            get;
            set;
        } = DateTime.Now;

        [Display(Name = "更新日時")]
        public DateTime UpdatedAt
        {
            get;
            set;
        } = DateTime.Now;
    }
}