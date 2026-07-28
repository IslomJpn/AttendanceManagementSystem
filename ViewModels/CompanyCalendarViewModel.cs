using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.ViewModels
{
    /// <summary>
    /// 会社カレンダー月別画面
    /// </summary>
    public class CompanyCalendarViewModel
    {
        /// <summary>
        /// 表示対象年
        /// </summary>
        public int TargetYear { get; set; }

        /// <summary>
        /// 表示対象月
        /// </summary>
        public int TargetMonth { get; set; }

        /// <summary>
        /// 対象月のカレンダーが作成済みか
        /// </summary>
        public bool IsGenerated { get; set; }

        /// <summary>
        /// 対象月の日付一覧
        /// </summary>
        public List<CompanyCalendarDayItemViewModel>
            Days
        { get; set; }
                = new();

        /// <summary>
        /// 所定労働日数
        /// </summary>
        public int WorkingDayCount =>
            Days.Count(d => d.IsWorkingDay);

        /// <summary>
        /// 休日数
        /// </summary>
        public int HolidayCount =>
            Days.Count(d => !d.IsWorkingDay);
    }

    /// <summary>
    /// カレンダー1日分の表示情報
    /// </summary>
    public class CompanyCalendarDayItemViewModel
    {
        public int CompanyCalendarDayId { get; set; }

        public DateTime CalendarDate { get; set; }

        public string DayType { get; set; }
            = string.Empty;

        public bool IsWorkingDay { get; set; }

        public string? HolidayName { get; set; }

        public string? Note { get; set; }

        /// <summary>
        /// 日本語の曜日
        /// </summary>
        public string DayOfWeekName =>
            CalendarDate.DayOfWeek switch
            {
                DayOfWeek.Sunday => "日",
                DayOfWeek.Monday => "月",
                DayOfWeek.Tuesday => "火",
                DayOfWeek.Wednesday => "水",
                DayOfWeek.Thursday => "木",
                DayOfWeek.Friday => "金",
                DayOfWeek.Saturday => "土",
                _ => ""
            };

        public bool IsSaturday =>
            CalendarDate.DayOfWeek ==
            DayOfWeek.Saturday;

        public bool IsSunday =>
            CalendarDate.DayOfWeek ==
            DayOfWeek.Sunday;
    }

    /// <summary>
    /// 会社カレンダー1日分の編集画面
    /// </summary>
    public class CompanyCalendarDayEditViewModel
    {
        public int CompanyCalendarDayId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "日付")]
        public DateTime CalendarDate { get; set; }

        [Required(ErrorMessage = "日付区分を選択してください。")]
        [StringLength(30)]
        [Display(Name = "日付区分")]
        public string DayType { get; set; }
            = "出勤日";

        [Display(Name = "所定労働日")]
        public bool IsWorkingDay { get; set; }
            = true;

        [StringLength(
            100,
            ErrorMessage =
                "休日・行事名は100文字以内で入力してください。")]
        [Display(Name = "休日・行事名")]
        public string? HolidayName { get; set; }

        [StringLength(
            500,
            ErrorMessage =
                "備考は500文字以内で入力してください。")]
        [Display(Name = "備考")]
        public string? Note { get; set; }
    }
}