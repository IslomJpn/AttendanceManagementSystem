using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    /// <summary>
    /// 会社カレンダーの1日分の設定を管理します。
    /// </summary>
    public class CompanyCalendarDay
    {
        public int CompanyCalendarDayId { get; set; }

        /// <summary>
        /// 対象日
        /// </summary>
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "日付")]
        public DateTime CalendarDate { get; set; }

        /// <summary>
        /// 日付区分
        /// 出勤日、会社休日、法定休日、祝日、
        /// 特別出勤日、年末年始休暇、夏季休暇
        /// </summary>
        [Required]
        [StringLength(30)]
        [Display(Name = "日付区分")]
        public string DayType { get; set; }
            = "出勤日";

        /// <summary>
        /// 所定労働日であるか
        /// true  = 出勤が必要な日
        /// false = 休日
        /// </summary>
        [Display(Name = "所定労働日")]
        public bool IsWorkingDay { get; set; }
            = true;

        /// <summary>
        /// 祝日名や会社休日名
        /// 例：元日、夏季休暇
        /// </summary>
        [StringLength(100)]
        [Display(Name = "休日・行事名")]
        public string? HolidayName { get; set; }

        /// <summary>
        /// 管理者用メモ
        /// </summary>
        [StringLength(500)]
        [Display(Name = "備考")]
        public string? Note { get; set; }

        [Display(Name = "登録日時")]
        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        [Display(Name = "更新日時")]
        public DateTime UpdatedAt { get; set; }
            = DateTime.Now;
    }
}