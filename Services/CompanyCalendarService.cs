using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagementSystem.Services
{
    /// <summary>
    /// 会社カレンダーの作成・取得・勤務日判定を行います。
    /// </summary>
    public class CompanyCalendarService
    {
        private readonly ApplicationDbContext _context;

        public CompanyCalendarService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 指定した年月の会社カレンダーを作成します。
        /// 既に登録済みの日付は変更しません。
        /// </summary>
        /// <returns>新しく作成した日数</returns>
        public async Task<int> GenerateMonthAsync(
            int year,
            int month)
        {
            ValidateYearAndMonth(
                year,
                month
            );

            var firstDate =
                new DateTime(
                    year,
                    month,
                    1
                );

            var lastDate =
                firstDate.AddMonths(1)
                    .AddDays(-1);

            var existingDates =
                await _context.CompanyCalendarDays
                    .AsNoTracking()
                    .Where(c =>
                        c.CalendarDate >= firstDate &&
                        c.CalendarDate <= lastDate)
                    .Select(c => c.CalendarDate.Date)
                    .ToHashSetAsync();

            var newCalendarDays =
                new List<CompanyCalendarDay>();

            var now =
                DateTime.Now;

            for (
                var date = firstDate;
                date <= lastDate;
                date = date.AddDays(1))
            {
                if (existingDates.Contains(
                        date.Date))
                {
                    continue;
                }

                var isWeekend =
                    date.DayOfWeek ==
                        DayOfWeek.Saturday ||
                    date.DayOfWeek ==
                        DayOfWeek.Sunday;

                newCalendarDays.Add(
                    new CompanyCalendarDay
                    {
                        CalendarDate =
                            date.Date,

                        DayType =
                            isWeekend
                                ? "会社休日"
                                : "出勤日",

                        IsWorkingDay =
                            !isWeekend,

                        HolidayName =
                            null,

                        Note =
                            null,

                        CreatedAt =
                            now,

                        UpdatedAt =
                            now
                    }
                );
            }

            if (newCalendarDays.Count == 0)
            {
                return 0;
            }

            await _context.CompanyCalendarDays
                .AddRangeAsync(
                    newCalendarDays
                );

            await _context.SaveChangesAsync();

            return newCalendarDays.Count;
        }

        /// <summary>
        /// 指定した年月の会社カレンダーを取得します。
        /// </summary>
        public async Task<List<CompanyCalendarDay>>
            GetMonthAsync(
                int year,
                int month)
        {
            ValidateYearAndMonth(
                year,
                month
            );

            var firstDate =
                new DateTime(
                    year,
                    month,
                    1
                );

            var nextMonth =
                firstDate.AddMonths(1);

            return await _context.CompanyCalendarDays
                .AsNoTracking()
                .Where(c =>
                    c.CalendarDate >= firstDate &&
                    c.CalendarDate < nextMonth)
                .OrderBy(c => c.CalendarDate)
                .ToListAsync();
        }

        /// <summary>
        /// 指定した日が会社の所定労働日か判定します。
        /// </summary>
        public async Task<bool> IsWorkingDayAsync(
            DateTime targetDate)
        {
            var calendarDate =
                targetDate.Date;

            var calendarDay =
                await _context.CompanyCalendarDays
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.CalendarDate ==
                        calendarDate);

            if (calendarDay != null)
            {
                return calendarDay.IsWorkingDay;
            }

            /*
             * 会社カレンダーがまだ作成されていない期間は、
             * システム停止を防ぐため暫定的に
             * 月曜日～金曜日を勤務日として扱います。
             *
             * カレンダー作成後は、
             * CompanyCalendarDays の設定が優先されます。
             */

            return
                calendarDate.DayOfWeek !=
                    DayOfWeek.Saturday &&
                calendarDate.DayOfWeek !=
                    DayOfWeek.Sunday;
        }

        /// <summary>
        /// 指定期間内の所定労働日数を計算します。
        /// </summary>
        public async Task<int> CountWorkingDaysAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var normalizedStartDate =
                startDate.Date;

            var normalizedEndDate =
                endDate.Date;

            if (normalizedEndDate <
                normalizedStartDate)
            {
                throw new ArgumentException(
                    "終了日は開始日以降の日付を指定してください。"
                );
            }

            var calendarDays =
                await _context.CompanyCalendarDays
                    .AsNoTracking()
                    .Where(c =>
                        c.CalendarDate >=
                            normalizedStartDate &&
                        c.CalendarDate <=
                            normalizedEndDate)
                    .ToDictionaryAsync(
                        c => c.CalendarDate.Date
                    );

            var workingDayCount =
                0;

            for (
                var date = normalizedStartDate;
                date <= normalizedEndDate;
                date = date.AddDays(1))
            {
                if (calendarDays.TryGetValue(
                        date.Date,
                        out var calendarDay))
                {
                    if (calendarDay.IsWorkingDay)
                    {
                        workingDayCount++;
                    }

                    continue;
                }

                var isWeekday =
                    date.DayOfWeek !=
                        DayOfWeek.Saturday &&
                    date.DayOfWeek !=
                        DayOfWeek.Sunday;

                if (isWeekday)
                {
                    workingDayCount++;
                }
            }

            return workingDayCount;
        }

        /// <summary>
        /// 年月の入力値を確認します。
        /// </summary>
        private static void ValidateYearAndMonth(
            int year,
            int month)
        {
            if (year < 2000 ||
                year > 2100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "年は2000年から2100年の範囲で指定してください。"
                );
            }

            if (month < 1 ||
                month > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    "月は1月から12月の範囲で指定してください。"
                );
            }
        }
    }
}