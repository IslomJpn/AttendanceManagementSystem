using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using AttendanceManagementSystem.Services;
using AttendanceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagementSystem.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext
    _context;

        private readonly OperationLogService
            _operationLogService;

        private readonly AttendanceStampLogService
            _attendanceStampLogService;

        private readonly MonthlyClosingService
            _monthlyClosingService;

        private readonly AttendanceCalculationService
            _attendanceCalculationService;

        private readonly CompanyCalendarService
            _companyCalendarService;

        public AttendanceController(
            ApplicationDbContext context,
            OperationLogService operationLogService,
            AttendanceStampLogService attendanceStampLogService,
            MonthlyClosingService monthlyClosingService,
            AttendanceCalculationService attendanceCalculationService,
            CompanyCalendarService companyCalendarService)
        {
            _context =
                context;

            _operationLogService =
                operationLogService;

            _attendanceStampLogService =
                attendanceStampLogService;

            _monthlyClosingService =
                monthlyClosingService;

            _attendanceCalculationService =
                attendanceCalculationService;

            _companyCalendarService =
                companyCalendarService;
        }

        public async Task<IActionResult> Index()
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var employeeId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Employee" ||
                employeeId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var employee =
                await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e =>
                        e.EmployeeId == employeeId.Value &&
                        e.IsActive
                    );

            if (employee == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            const int missingStampGraceMinutes = 15;

            var today =
                DateTime.Today;

            var now =
                DateTime.Now;

            var nowTime =
                now.TimeOfDay;

            var attendance =
                await _context.Attendances
                    .FirstOrDefaultAsync(a =>
                        a.EmployeeId == employeeId.Value &&
                        a.AttendanceDate == today
                    );

            var paidLeaveBalance =
                await _context.PaidLeaveBalances
                    .FirstOrDefaultAsync(p =>
                        p.EmployeeId == employeeId.Value &&
                        p.Year == today.Year
                    );

            // 会社カレンダーを基準に勤務日を判定
            var isCompanyWorkingDay =
                await _companyCalendarService
                    .IsWorkingDayAsync(today);
            // 本日の会社カレンダー情報
            var todayCalendarDay =
                await _context.CompanyCalendarDays
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.CalendarDate == today
                    );

            var todayDayType =
                todayCalendarDay?.DayType ??
                (
                    isCompanyWorkingDay
                        ? "出勤日"
                        : "会社休日"
                );

            var todayHolidayName =
                todayCalendarDay?.HolidayName;

            // 承認済み有給日は未打刻アラート対象外
            var hasApprovedPaidLeave =
                await _context.PaidLeaveRequests
                    .AsNoTracking()
                    .AnyAsync(p =>
                        p.EmployeeId == employeeId.Value &&
                        p.LeaveDate == today &&
                        p.Status == "承認"
                    );

            var viewModel =
                new AttendanceIndexViewModel
                {
                    EmployeeName =
                        employee.Name,

                    DepartmentName =
                        employee.Department
                            ?.DepartmentName ?? "",

                    Today =
                        today,

                    CurrentTime =
                        now,

                    RemainingPaidLeaveDays =
                        paidLeaveBalance
                            ?.RemainingDays ?? 0,

                    Message =
                        TempData["Message"] as string,
                    // 本日の会社カレンダー
                    IsCompanyWorkingDay =
    isCompanyWorkingDay,

                    TodayDayType =
    todayDayType,

                    TodayHolidayName =
    todayHolidayName,

                    // 勤怠データがある場合は
                    // 出勤時に保存した勤務条件を使用
                    ScheduledStartTime =
                        attendance != null
                            ? attendance
                                .ScheduledStartTimeSnapshot
                            : employee
                                .ScheduledStartTime,

                    ScheduledEndTime =
                        attendance != null
                            ? attendance
                                .ScheduledEndTimeSnapshot
                            : employee
                                .ScheduledEndTime,

                    ScheduledWorkMinutes =
                        attendance != null
                            ? attendance
                                .ScheduledWorkMinutesSnapshot
                            : employee
                                .ScheduledWorkMinutes,

                    LunchBreakStartTime =
                        attendance != null
                            ? attendance
                                .LunchBreakStartTimeSnapshot
                            : employee
                                .LunchBreakStartTime,

                    LunchBreakEndTime =
                        attendance != null
                            ? attendance
                                .LunchBreakEndTimeSnapshot
                            : employee
                                .LunchBreakEndTime,

                    SmallBreak1StartTime =
                        attendance != null
                            ? attendance
                                .SmallBreak1StartTimeSnapshot
                            : employee
                                .SmallBreak1StartTime,

                    SmallBreak1EndTime =
                        attendance != null
                            ? attendance
                                .SmallBreak1EndTimeSnapshot
                            : employee
                                .SmallBreak1EndTime,

                    SmallBreak2StartTime =
                        attendance != null
                            ? attendance
                                .SmallBreak2StartTimeSnapshot
                            : employee
                                .SmallBreak2StartTime,

                    SmallBreak2EndTime =
                        attendance != null
                            ? attendance
                                .SmallBreak2EndTimeSnapshot
                            : employee
                                .SmallBreak2EndTime
                };

            if (attendance != null)
            {
                viewModel.ClockInTime =
                    attendance.ClockInTime;

                viewModel.ClockOutTime =
                    attendance.ClockOutTime;

                viewModel.BreakMinutes =
                    attendance.BreakMinutes;

                viewModel.WorkMinutes =
                    attendance.WorkMinutes;

                viewModel.LateMinutes =
                    attendance.LateMinutes;

                viewModel.OvertimeMinutes =
                    attendance.OvertimeMinutes;

                viewModel.Status =
                    attendance.Status;

                // 出勤中は現在時刻までリアルタイム計算
                if (attendance.ClockInTime.HasValue &&
                    !attendance.ClockOutTime.HasValue)
                {
                    var calculation =
                        _attendanceCalculationService
                            .Calculate(
                                attendance,
                                attendance.ClockInTime.Value,
                                nowTime
                            );

                    viewModel.BreakMinutes =
                        calculation.BreakMinutes;

                    viewModel.WorkMinutes =
                        calculation.WorkMinutes;

                    viewModel.LateMinutes =
                        calculation.LateMinutes;

                    viewModel.OvertimeMinutes =
                        calculation.OvertimeMinutes;
                }
            }

            // 会社の所定労働日で、
            // 承認済み有給ではない場合だけ判定する
            if (isCompanyWorkingDay &&
                !hasApprovedPaidLeave)
            {
                var clockInAlertTime =
                    viewModel.ScheduledStartTime.Add(
                        TimeSpan.FromMinutes(
                            missingStampGraceMinutes
                        )
                    );

                var clockOutAlertTime =
                    viewModel.ScheduledEndTime.Add(
                        TimeSpan.FromMinutes(
                            missingStampGraceMinutes
                        )
                    );

                if (!viewModel.ClockInTime.HasValue &&
                    nowTime >= clockInAlertTime)
                {
                    var elapsedMinutes =
                        Math.Max(
                            0,
                            (int)(
                                nowTime -
                                viewModel.ScheduledStartTime
                            ).TotalMinutes
                        );

                    var elapsedTimeText =
                        FormatElapsedTime(
                            elapsedMinutes
                        );

                    viewModel.IsClockInMissingAlert =
                        true;

                    viewModel.AttendanceAlertMessage =
                        $"所定出勤時刻の" +
                        $"{viewModel.ScheduledStartTime:hh\\:mm}から" +
                        $"{elapsedTimeText}経過しています。" +
                        "出勤打刻を確認してください。";
                }
                else if (
                    viewModel.ClockInTime.HasValue &&
                    !viewModel.ClockOutTime.HasValue &&
                    nowTime >= clockOutAlertTime)
                {
                    var elapsedMinutes =
                        Math.Max(
                            0,
                            (int)(
                                nowTime -
                                viewModel.ScheduledEndTime
                            ).TotalMinutes
                        );

                    var elapsedTimeText =
                        FormatElapsedTime(
                            elapsedMinutes
                        );

                    viewModel.IsClockOutMissingAlert =
                        true;

                    viewModel.AttendanceAlertMessage =
                        $"所定退勤時刻の" +
                        $"{viewModel.ScheduledEndTime:hh\\:mm}から" +
                        $"{elapsedTimeText}経過しています。" +
                        "退勤打刻を確認してください。";
                }
            }

            return View(viewModel);
        }


        // =====================================
        // 社員用会社カレンダー
        // =====================================

        [HttpGet]
        public async Task<IActionResult> CompanyCalendar(
            string? yearMonth)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var employeeId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Employee" ||
                employeeId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var targetMonth =
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1
                );

            if (!string.IsNullOrWhiteSpace(yearMonth))
            {
                var parsed =
                    DateTime.TryParseExact(
                        yearMonth,
                        "yyyy-MM",
                        System.Globalization
                            .CultureInfo
                            .InvariantCulture,
                        System.Globalization
                            .DateTimeStyles
                            .None,
                        out var parsedMonth
                    );

                if (parsed &&
                    parsedMonth.Year >= 2000 &&
                    parsedMonth.Year <= 2100)
                {
                    targetMonth =
                        new DateTime(
                            parsedMonth.Year,
                            parsedMonth.Month,
                            1
                        );
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "対象年月の形式が正しくありません。";
                }
            }

            var calendarDays =
                await _companyCalendarService
                    .GetMonthAsync(
                        targetMonth.Year,
                        targetMonth.Month
                    );

            var expectedDayCount =
                DateTime.DaysInMonth(
                    targetMonth.Year,
                    targetMonth.Month
                );

            var viewModel =
                new CompanyCalendarViewModel
                {
                    TargetYear =
                        targetMonth.Year,

                    TargetMonth =
                        targetMonth.Month,

                    IsGenerated =
                        calendarDays.Count ==
                        expectedDayCount,

                    Days =
                        calendarDays
                            .Select(day =>
                                new CompanyCalendarDayItemViewModel
                                {
                                    CompanyCalendarDayId =
                                        day.CompanyCalendarDayId,

                                    CalendarDate =
                                        day.CalendarDate,

                                    DayType =
                                        day.DayType,

                                    IsWorkingDay =
                                        day.IsWorkingDay,

                                    HolidayName =
                                        day.HolidayName,

                                    Note =
                                        day.Note
                                })
                            .OrderBy(day =>
                                day.CalendarDate)
                            .ToList()
                };

            return View(viewModel);
        }

        public async Task<IActionResult> History(
    string? yearMonth)
        {
            var role =
                HttpContext.Session.GetString(
                    "LoginUserRole"
                );

            var employeeId =
                HttpContext.Session.GetInt32(
                    "LoginUserId"
                );

            if (role != "Employee" ||
                employeeId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var employee =
                await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.EmployeeId ==
                                employeeId.Value &&
                            item.IsActive
                    );

            if (employee == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var targetMonth =
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1
                );

            if (!string.IsNullOrWhiteSpace(
                    yearMonth))
            {
                var parsed =
                    DateTime.TryParseExact(
                        yearMonth,
                        "yyyy-MM",
                        System.Globalization
                            .CultureInfo
                            .InvariantCulture,
                        System.Globalization
                            .DateTimeStyles
                            .None,
                        out var parsedMonth
                    );

                if (parsed)
                {
                    targetMonth =
                        new DateTime(
                            parsedMonth.Year,
                            parsedMonth.Month,
                            1
                        );
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "対象年月の形式が正しくありません。";
                }
            }

            var startDate =
                targetMonth;

            var endDate =
                startDate.AddMonths(1);

            var today =
                DateTime.Today;

            // =====================================
            // 会社カレンダー
            // =====================================

            var companyCalendarRows =
                await _context.CompanyCalendarDays
                    .AsNoTracking()
                    .Where(calendarDay =>
                        calendarDay.CalendarDate >=
                            startDate &&
                        calendarDay.CalendarDate <
                            endDate)
                    .ToListAsync();

            var companyCalendarLookup =
                companyCalendarRows
                    .GroupBy(calendarDay =>
                        calendarDay.CalendarDate.Date)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group
                                .OrderByDescending(
                                    calendarDay =>
                                        calendarDay
                                            .CompanyCalendarDayId
                                )
                                .First()
                    );

            bool IsCompanyWorkingDay(
                DateTime date)
            {
                date =
                    date.Date;

                if (companyCalendarLookup.TryGetValue(
                        date,
                        out var calendarDay))
                {
                    return calendarDay
                        .IsWorkingDay;
                }

                return
                    date.DayOfWeek !=
                        DayOfWeek.Saturday &&
                    date.DayOfWeek !=
                        DayOfWeek.Sunday;
            }

            // =====================================
            // 勤怠データ
            // =====================================

            var attendanceRows =
                await _context.Attendances
                    .AsNoTracking()
                    .Where(attendance =>
                        attendance.EmployeeId ==
                            employeeId.Value &&
                        attendance.AttendanceDate >=
                            startDate &&
                        attendance.AttendanceDate <
                            endDate)
                    .ToListAsync();

            var attendanceLookup =
                attendanceRows
                    .GroupBy(attendance =>
                        attendance.AttendanceDate.Date)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group
                                .OrderByDescending(
                                    attendance =>
                                        attendance.UpdatedAt
                                )
                                .ThenByDescending(
                                    attendance =>
                                        attendance.AttendanceId
                                )
                                .First()
                    );

            // =====================================
            // 承認済み有給
            // =====================================

            var approvedPaidLeaveRows =
                await _context.PaidLeaveRequests
                    .AsNoTracking()
                    .Where(request =>
                        request.EmployeeId ==
                            employeeId.Value &&
                        request.Status ==
                            "承認" &&
                        request.Days >
                            0 &&
                        request.LeaveDate >=
                            startDate &&
                        request.LeaveDate <
                            endDate)
                    .ToListAsync();

            var approvedPaidLeaveDates =
                approvedPaidLeaveRows
                    .Select(request =>
                        request.LeaveDate.Date)
                    .ToHashSet();

            // =====================================
            // 欠勤確定者
            // =====================================

            var confirmerIds =
                attendanceRows
                    .Where(attendance =>
                        attendance.AbsenceConfirmedBy
                            .HasValue)
                    .Select(attendance =>
                        attendance.AbsenceConfirmedBy!
                            .Value)
                    .Distinct()
                    .ToList();

            var confirmerNames =
                await _context.Employees
                    .AsNoTracking()
                    .Where(item =>
                        confirmerIds.Contains(
                            item.EmployeeId
                        ))
                    .ToDictionaryAsync(
                        item =>
                            item.EmployeeId,
                        item =>
                            item.Name
                    );

            // =====================================
            // 表示対象日
            // =====================================

            var displayDates =
                new HashSet<DateTime>();

            var finalWorkingDate =
                endDate.AddDays(-1);

            if (finalWorkingDate > today)
            {
                finalWorkingDate =
                    today;
            }

            var currentDate =
                startDate;

            while (currentDate <=
                   finalWorkingDate)
            {
                if (IsCompanyWorkingDay(
                        currentDate))
                {
                    displayDates.Add(
                        currentDate
                    );
                }

                currentDate =
                    currentDate.AddDays(1);
            }

            // 休日出勤など、勤怠記録がある日も表示する
            foreach (var attendanceDate
                     in attendanceRows.Select(
                         attendance =>
                             attendance
                                 .AttendanceDate
                                 .Date))
            {
                displayDates.Add(
                    attendanceDate
                );
            }

            // 将来の承認済み有給予定日も表示する
            foreach (var paidLeaveDate
                     in approvedPaidLeaveDates)
            {
                displayDates.Add(
                    paidLeaveDate
                );
            }

            var items =
                new List<
                    AttendanceHistoryItemViewModel>();

            foreach (var attendanceDate
                     in displayDates)
            {
                attendanceLookup.TryGetValue(
                    attendanceDate,
                    out var attendance
                );

                var hasApprovedPaidLeave =
                    approvedPaidLeaveDates.Contains(
                        attendanceDate
                    );

                var isCompanyWorkingDay =
                    IsCompanyWorkingDay(
                        attendanceDate
                    );

                string status;

                if (attendance?.IsAbsent ==
                    true)
                {
                    status =
                        "欠勤";
                }
                else if (attendance != null)
                {
                    status =
                        string.IsNullOrWhiteSpace(
                            attendance.Status)
                            ? "未打刻"
                            : attendance.Status;
                }
                else if (hasApprovedPaidLeave)
                {
                    status =
                        attendanceDate > today
                            ? "有給予定"
                            : "有給";
                }
                else if (isCompanyWorkingDay)
                {
                    status =
                        "未打刻";
                }
                else
                {
                    status =
                        "休日";
                }

                var confirmedByName =
                    string.Empty;

                if (attendance
                        ?.AbsenceConfirmedBy
                        .HasValue ==
                    true)
                {
                    confirmerNames.TryGetValue(
                        attendance
                            .AbsenceConfirmedBy
                            .Value,
                        out confirmedByName
                    );
                }

                items.Add(
                    new AttendanceHistoryItemViewModel
                    {
                        AttendanceId =
                            attendance
                                ?.AttendanceId,

                        AttendanceDate =
                            attendanceDate,

                        ClockInTime =
                            attendance
                                ?.ClockInTime,

                        ClockOutTime =
                            attendance
                                ?.ClockOutTime,

                        BreakMinutes =
                            attendance
                                ?.BreakMinutes ??
                            0,

                        WorkMinutes =
                            attendance
                                ?.WorkMinutes ??
                            0,

                        LateMinutes =
                            attendance
                                ?.LateMinutes ??
                            0,

                        OvertimeMinutes =
                            attendance
                                ?.OvertimeMinutes ??
                            0,

                        Status =
                            status,

                        IsCompanyWorkingDay =
                            isCompanyWorkingDay,

                        HasAttendanceRecord =
                            attendance != null,

                        HasApprovedPaidLeave =
                            hasApprovedPaidLeave,

                        IsAbsent =
                            attendance
                                ?.IsAbsent ??
                            false,

                        AbsenceReason =
                            attendance
                                ?.AbsenceReason ??
                            string.Empty,

                        AbsenceConfirmedAt =
                            attendance
                                ?.AbsenceConfirmedAt,

                        AbsenceConfirmedBy =
                            attendance
                                ?.AbsenceConfirmedBy,

                        AbsenceConfirmedByName =
                            confirmedByName ??
                            string.Empty
                    }
                );
            }

            var viewModel =
                new AttendanceHistoryViewModel
                {
                    YearMonth =
                        startDate.ToString(
                            "yyyy-MM"
                        ),

                    Items =
                        items
                            .OrderByDescending(item =>
                                item.AttendanceDate)
                            .ToList()
                };

            return View(
                viewModel
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockIn(
            decimal? latitude,
            decimal? longitude,
            double? accuracyMeters,
            string? gpsStatus)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var employeeId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Employee" || employeeId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = _context.Employees
                .FirstOrDefault(e =>
                    e.EmployeeId == employeeId.Value &&
                    e.IsActive);

            if (employee == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction("Login", "Account");
            }

            var today = DateTime.Today;
            // 会社カレンダーで本日が勤務日か確認する
            var isCompanyWorkingDay =
                await _companyCalendarService
                    .IsWorkingDayAsync(today);

            if (!isCompanyWorkingDay)
            {
                var calendarDay =
                    await _context.CompanyCalendarDays
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c =>
                            c.CalendarDate == today
                        );

                var dayType =
                    calendarDay?.DayType ??
                    "会社休日";

                var holidayName =
                    calendarDay?.HolidayName;

                var holidayText =
                    string.IsNullOrWhiteSpace(holidayName)
                        ? dayType
                        : $"{dayType}（{holidayName}）";

                _operationLogService.Write(
                    actionName: "出勤打刻",
                    targetType: "CompanyCalendarDay",
                    targetId:
                        calendarDay?.CompanyCalendarDayId,
                    details:
                        $"会社カレンダー上の休日のため、" +
                        $"出勤打刻を拒否しました。" +
                        $"対象日：{today:yyyy/MM/dd}、" +
                        $"日付区分：{holidayText}。",
                    result: "失敗"
                );

                TempData["Message"] =
                    $"本日は{holidayText}のため、" +
                    "出勤打刻できません。";

                return RedirectToAction("Index");
            }

            if (_monthlyClosingService.IsClosed(today))
            {
                _operationLogService.Write(
                    actionName: "出勤打刻",
                    targetType: "MonthlyClosing",
                    details:
                        $"月次締め済みのため、出勤打刻を拒否しました。" +
                        $"対象日：{today:yyyy/MM/dd}。",
                    result: "失敗"
                );

                TempData["Message"] =
                    $"{today:yyyy年MM月}は月次締め済みのため、" +
                    "出勤打刻できません。";

                return RedirectToAction("Index");
            }

            var normalizedGpsStatus =
                NormalizeGpsStatus(gpsStatus);

            var nowDateTime = DateTime.Now;
            var nowTime = nowDateTime.TimeOfDay;

            var attendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.EmployeeId == employeeId.Value &&
                    a.AttendanceDate == today);

            if (attendance != null &&
                attendance.ClockInTime.HasValue)
            {
                _operationLogService.Write(
                    actionName: "出勤打刻",
                    targetType: "Attendance",
                    targetId: attendance.AttendanceId,
                    details:
                        $"すでに出勤打刻済みです。" +
                        $"打刻日：{today:yyyy/MM/dd}、" +
                        $"出勤時刻：" +
                        $"{attendance.ClockInTime.Value:hh\\:mm}",
                    result: "失敗"
                );

                TempData["Message"] =
                    "すでに出勤登録されています。";

                return RedirectToAction("Index");
            }

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    EmployeeId =
                        employeeId.Value,

                    AttendanceDate =
                        today,

                    ClockInTime =
                        nowTime,

                    ClockOutTime =
                        null,

                    BreakMinutes = 0,
                    WorkMinutes = 0,
                    LateMinutes = 0,
                    OvertimeMinutes = 0,

                    Status =
                        "出勤中",

                    CreatedAt =
                        nowDateTime,

                    UpdatedAt =
                        nowDateTime
                };

                _context.Attendances.Add(attendance);
            }
            else
            {
                attendance.ClockInTime =
                    nowTime;

                attendance.ClockOutTime =
                    null;

                attendance.BreakMinutes = 0;
                attendance.WorkMinutes = 0;
                attendance.LateMinutes = 0;
                attendance.OvertimeMinutes = 0;

                attendance.Status =
                    "出勤中";

                attendance.UpdatedAt =
                    nowDateTime;
            }

            // 打刻日時点の勤務条件を保存
            _attendanceCalculationService
                .ApplyScheduleSnapshot(
                    employee,
                    attendance
                );

            // 保存したスナップショットで遅刻時間を計算
            var lateMinutes =
                _attendanceCalculationService
                    .CalculateLateMinutes(
                        attendance,
                        nowTime
                    );

            attendance.LateMinutes =
                lateMinutes;

            attendance.Status =
                lateMinutes > 0
                    ? "遅刻"
                    : "出勤中";

            _context.SaveChanges();

            _attendanceStampLogService.Write(
                attendanceId: attendance.AttendanceId,
                employeeId: employeeId.Value,
                stampType: "出勤",
                latitude: latitude,
                longitude: longitude,
                accuracyMeters: accuracyMeters,
                gpsStatus: normalizedGpsStatus,
                result: "成功",
                details:
                    $"出勤打刻を記録しました。" +
                    $"打刻日：{today:yyyy/MM/dd}、" +
                    $"打刻時刻：{nowTime:hh\\:mm}。"
            );

            _operationLogService.Write(
                actionName: "出勤打刻",
                targetType: "Attendance",
                targetId: attendance.AttendanceId,
                details:
                    $"出勤打刻を行いました。" +
                    $"打刻日：{today:yyyy/MM/dd}、" +
                    $"出勤時刻：{nowTime:hh\\:mm}、" +
                    $"所定出勤時刻：" +
                    $"{attendance.ScheduledStartTimeSnapshot:hh\\:mm}、" +
                    $"遅刻時間：{lateMinutes}分。",
                result: "成功"
            );

            TempData["Message"] =
                "出勤登録しました。";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClockOut(
            decimal? latitude,
            decimal? longitude,
            double? accuracyMeters,
            string? gpsStatus)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var employeeId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Employee" || employeeId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = _context.Employees
                .FirstOrDefault(e =>
                    e.EmployeeId == employeeId.Value &&
                    e.IsActive);

            if (employee == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction("Login", "Account");
            }

            var today = DateTime.Today;

            if (_monthlyClosingService.IsClosed(today))
            {
                _operationLogService.Write(
                    actionName: "退勤打刻",
                    targetType: "MonthlyClosing",
                    details:
                        $"月次締め済みのため、退勤打刻を拒否しました。" +
                        $"対象日：{today:yyyy/MM/dd}。",
                    result: "失敗"
                );

                TempData["Message"] =
                    $"{today:yyyy年MM月}は月次締め済みのため、" +
                    "退勤打刻できません。";

                return RedirectToAction("Index");
            }

            var normalizedGpsStatus =
                NormalizeGpsStatus(gpsStatus);

            var attendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.EmployeeId == employeeId.Value &&
                    a.AttendanceDate == today);

            if (attendance == null ||
                !attendance.ClockInTime.HasValue)
            {
                _operationLogService.Write(
                    actionName: "退勤打刻",
                    targetType: "Attendance",
                    details:
                        $"出勤打刻が存在しないため、" +
                        $"退勤打刻を拒否しました。" +
                        $"対象日：{today:yyyy/MM/dd}",
                    result: "失敗"
                );

                TempData["Message"] =
                    "出勤登録がありません。";

                return RedirectToAction("Index");
            }

            if (attendance.ClockOutTime.HasValue)
            {
                _operationLogService.Write(
                    actionName: "退勤打刻",
                    targetType: "Attendance",
                    targetId: attendance.AttendanceId,
                    details:
                        $"すでに退勤打刻済みです。" +
                        $"対象日：{today:yyyy/MM/dd}、" +
                        $"退勤時刻：" +
                        $"{attendance.ClockOutTime.Value:hh\\:mm}",
                    result: "失敗"
                );

                TempData["Message"] =
                    "すでに退勤登録されています。";

                return RedirectToAction("Index");
            }

            var nowDateTime = DateTime.Now;
            var nowTime = nowDateTime.TimeOfDay;

            if (nowTime <= attendance.ClockInTime.Value)
            {
                _operationLogService.Write(
                    actionName: "退勤打刻",
                    targetType: "Attendance",
                    targetId: attendance.AttendanceId,
                    details:
                        $"退勤時刻が出勤時刻以前のため、" +
                        $"退勤打刻を拒否しました。" +
                        $"出勤時刻：" +
                        $"{attendance.ClockInTime.Value:hh\\:mm}、" +
                        $"退勤時刻：{nowTime:hh\\:mm}。",
                    result: "失敗"
                );

                TempData["Message"] =
                    "退勤時刻が出勤時刻以前のため、" +
                    "退勤登録できません。";

                return RedirectToAction("Index");
            }

            var calculation =
            _attendanceCalculationService.Calculate(
              attendance,
                attendance.ClockInTime.Value,
               nowTime
                     );

            attendance.ClockOutTime =
                nowTime;

            attendance.BreakMinutes =
                calculation.BreakMinutes;

            attendance.WorkMinutes =
                calculation.WorkMinutes;

            attendance.LateMinutes =
                calculation.LateMinutes;

            attendance.OvertimeMinutes =
                calculation.OvertimeMinutes;

            attendance.Status =
                "退勤済み";

            attendance.UpdatedAt =
                nowDateTime;

            _context.SaveChanges();

            _attendanceStampLogService.Write(
                attendanceId: attendance.AttendanceId,
                employeeId: employeeId.Value,
                stampType: "退勤",
                latitude: latitude,
                longitude: longitude,
                accuracyMeters: accuracyMeters,
                gpsStatus: normalizedGpsStatus,
                result: "成功",
                details:
                    $"退勤打刻を記録しました。" +
                    $"打刻日：{today:yyyy/MM/dd}、" +
                    $"打刻時刻：{nowTime:hh\\:mm}。"
            );

            _operationLogService.Write(
                actionName: "退勤打刻",
                targetType: "Attendance",
                targetId: attendance.AttendanceId,
                details:
                    $"退勤打刻を行いました。" +
                    $"対象日：{today:yyyy/MM/dd}、" +
                    $"出勤時刻：" +
                    $"{attendance.ClockInTime.Value:hh\\:mm}、" +
                    $"退勤時刻：{nowTime:hh\\:mm}、" +
                    $"休憩時間：{attendance.BreakMinutes}分、" +
                    $"実働時間：{attendance.WorkMinutes}分、" +
                    $"所定労働時間：" +
                    $"{attendance.ScheduledWorkMinutesSnapshot}分、" +
                    $"残業時間：" +
                    $"{attendance.OvertimeMinutes}分。",
                result: "成功"
            );

            TempData["Message"] =
                "退勤登録しました。";

            return RedirectToAction("Index");
        }

        private static string FormatElapsedTime(
            int totalMinutes)
        {
            var safeMinutes =
                Math.Max(
                    0,
                    totalMinutes
                );

            var hours =
                safeMinutes / 60;

            var minutes =
                safeMinutes % 60;

            if (hours <= 0)
            {
                return $"{minutes}分";
            }

            if (minutes <= 0)
            {
                return $"{hours}時間";
            }

            return $"{hours}時間{minutes}分";
        }

        private static string NormalizeGpsStatus(
            string? gpsStatus)
        {
            if (string.IsNullOrWhiteSpace(gpsStatus))
            {
                return "未取得";
            }

            return gpsStatus.Trim();
        }
    }
}