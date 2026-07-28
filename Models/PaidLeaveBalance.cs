using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class PaidLeaveBalance
    {
        [Key]
        public int PaidLeaveBalanceId
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

        // 現在の残高情報を管理する年度
        [Required]
        [Display(Name = "管理年度")]
        public int Year
        {
            get;
            set;
        }

        // =====================================
        // 当期付与・繰越・失効
        // =====================================

        [Display(Name = "当期付与日数")]
        public double CurrentGrantedDays
        {
            get;
            set;
        } = 0.0;

        [Display(Name = "繰越日数")]
        public double CarriedOverDays
        {
            get;
            set;
        } = 0.0;

        [Display(Name = "失効日数")]
        public double ExpiredDays
        {
            get;
            set;
        } = 0.0;

        // 当期付与日数と有効な繰越日数の合計
        [Display(Name = "利用可能総日数")]
        public double GrantedDays
        {
            get;
            set;
        } = 0.0;

        [Display(Name = "使用日数")]
        public double UsedDays
        {
            get;
            set;
        } = 0.0;

        [Display(Name = "承認済み予定日数")]
        public double ReservedDays
        {
            get;
            set;
        } = 0.0;

        [Display(Name = "残日数")]
        public double RemainingDays
        {
            get;
            set;
        } = 0.0;

        // =====================================
        // 現在の付与情報
        // =====================================

        [DataType(DataType.Date)]
        [Display(Name = "現在の付与日")]
        public DateTime? CurrentGrantDate
        {
            get;
            set;
        }

        [DataType(DataType.Date)]
        [Display(Name = "現在付与分の有効期限")]
        public DateTime? CurrentGrantExpiryDate
        {
            get;
            set;
        }

        // =====================================
        // 管理情報
        // =====================================

        [Display(Name = "最終計算日時")]
        public DateTime? LastCalculatedAt
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