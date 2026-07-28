using System.Globalization;
using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Helpers;
using AttendanceManagementSystem.Models;
using AttendanceManagementSystem.Services;
using AttendanceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly PaidLeaveRuleService
            _paidLeaveRuleService;

        private readonly OperationLogService
            _operationLogService;

        private readonly MonthlyClosingService
            _monthlyClosingService;

        private readonly AttendanceCalculationService
            _attendanceCalculationService;

        private readonly CompanyCalendarService
            _companyCalendarService;
        private readonly PaidLeaveGrantHistoryService
    _paidLeaveGrantHistoryService;

        private readonly PaidLeaveBalanceCalculationService
            _paidLeaveBalanceCalculationService;

        public AdminController(
            ApplicationDbContext context,
            PaidLeaveRuleService paidLeaveRuleService,
            OperationLogService operationLogService,
            MonthlyClosingService monthlyClosingService,
           AttendanceCalculationService attendanceCalculationService,
           CompanyCalendarService companyCalendarService,
PaidLeaveGrantHistoryService paidLeaveGrantHistoryService,
PaidLeaveBalanceCalculationService paidLeaveBalanceCalculationService)
        {
            _context = context;

            _paidLeaveRuleService =
                paidLeaveRuleService;

            _operationLogService =
                operationLogService;

            _monthlyClosingService =
                monthlyClosingService;

            _attendanceCalculationService =
                attendanceCalculationService;

            _companyCalendarService =
                companyCalendarService;
            _paidLeaveGrantHistoryService =
             paidLeaveGrantHistoryService;

            _paidLeaveBalanceCalculationService =
                paidLeaveBalanceCalculationService;
        }

        public async Task<IActionResult> Index()
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
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

            var currentTime =
                now.TimeOfDay;

            // 会社カレンダーを基準に勤務日を判定する
            var isCompanyWorkingDay =
                await _companyCalendarService
                    .IsWorkingDayAsync(today);

            // 本日の確認対象となる有効な社員
            var activeEmployees =
                await _context.Employees
                    .AsNoTracking()
                    .Include(e => e.Department)
                    .Where(e =>
                        e.IsActive &&
                        e.Role == "Employee" &&
                        e.JoinDate <= today)
                    .OrderBy(e => e.EmployeeId)
                    .ToListAsync();

            // 本日の勤怠データ
            var todayAttendances =
                await _context.Attendances
                    .AsNoTracking()
                    .Where(a =>
                        a.AttendanceDate == today)
                    .ToListAsync();

            // 本日の承認済み有給社員
            var paidLeaveEmployeeIds =
                (await _context.PaidLeaveRequests
                    .AsNoTracking()
                    .Where(r =>
                        r.LeaveDate == today &&
                        r.Status == "承認")
                    .Select(r => r.EmployeeId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            var missingClockInEmployees =
                new List<MissingClockInEmployeeViewModel>();

            var missingClockOutEmployees =
                new List<MissingClockOutEmployeeViewModel>();

            // 会社カレンダーで所定労働日の場合だけ
            // 未打刻判定を実行する
            if (isCompanyWorkingDay)
            {
                foreach (var employee in activeEmployees)
                {
                    // 承認済み有給の場合は対象外
                    if (paidLeaveEmployeeIds.Contains(
                            employee.EmployeeId))
                    {
                        continue;
                    }

                    var attendance =
                        todayAttendances.FirstOrDefault(a =>
                            a.EmployeeId ==
                            employee.EmployeeId);

                    // =====================================
                    // 出勤未打刻
                    // =====================================

                    if (attendance?.ClockInTime == null)
                    {
                        var alertStartTime =
                            employee.ScheduledStartTime.Add(
                                TimeSpan.FromMinutes(
                                    missingStampGraceMinutes
                                )
                            );

                        if (currentTime >= alertStartTime)
                        {
                            var elapsedMinutes =
                                Math.Max(
                                    0,
                                    (int)(
                                        currentTime -
                                        employee.ScheduledStartTime
                                    ).TotalMinutes
                                );

                            missingClockInEmployees.Add(
                                new MissingClockInEmployeeViewModel
                                {
                                    EmployeeId =
                                        employee.EmployeeId,

                                    EmployeeName =
                                        employee.Name,

                                    DepartmentName =
                                        employee.Department
                                            ?.DepartmentName ?? "",

                                    ScheduledStartTime =
                                        employee.ScheduledStartTime,

                                    ElapsedMinutes =
                                        elapsedMinutes
                                }
                            );
                        }

                        continue;
                    }

                    // =====================================
                    // 退勤未打刻
                    // =====================================

                    if (!attendance.ClockOutTime.HasValue)
                    {
                        var scheduledEndTime =
                            attendance
                                .ScheduledEndTimeSnapshot;

                        var alertEndTime =
                            scheduledEndTime.Add(
                                TimeSpan.FromMinutes(
                                    missingStampGraceMinutes
                                )
                            );

                        if (currentTime >= alertEndTime)
                        {
                            var elapsedMinutes =
                                Math.Max(
                                    0,
                                    (int)(
                                        currentTime -
                                        scheduledEndTime
                                    ).TotalMinutes
                                );

                            missingClockOutEmployees.Add(
                                new MissingClockOutEmployeeViewModel
                                {
                                    EmployeeId =
                                        employee.EmployeeId,

                                    EmployeeName =
                                        employee.Name,

                                    DepartmentName =
                                        employee.Department
                                            ?.DepartmentName ?? "",

                                    ClockInTime =
                                        attendance
                                            .ClockInTime.Value,

                                    ScheduledEndTime =
                                        scheduledEndTime,

                                    ElapsedMinutes =
                                        elapsedMinutes
                                }
                            );
                        }
                    }
                }
            }

            var activeEmployeeIds =
                activeEmployees
                    .Select(e => e.EmployeeId)
                    .ToHashSet();

            var paidLeaveAlertItems =
                GetPaidLeaveAlertItems();

            var viewModel =
                new AdminIndexViewModel
                {
                    TodayClockInCount =
                        todayAttendances.Count(a =>
                            activeEmployeeIds.Contains(
                                a.EmployeeId
                            ) &&
                            a.ClockInTime.HasValue),

                    TodayLateCount =
                        todayAttendances.Count(a =>
                            activeEmployeeIds.Contains(
                                a.EmployeeId
                            ) &&
                            a.LateMinutes > 0),

                    TodayClockOutCount =
                        todayAttendances.Count(a =>
                            activeEmployeeIds.Contains(
                                a.EmployeeId
                            ) &&
                            a.ClockOutTime.HasValue),

                    PendingCorrectionRequestCount =
                        await _context
                            .AttendanceCorrectionRequests
                            .CountAsync(r =>
                                r.Status == "申請中"),

                    PendingPaidLeaveRequestCount =
                        await _context.PaidLeaveRequests
                            .CountAsync(r =>
                                r.Status == "申請中"),

                    PaidLeaveAlertCount =
                        paidLeaveAlertItems.Count,

                    MissingClockInEmployees =
                        missingClockInEmployees,

                    MissingClockOutEmployees =
                        missingClockOutEmployees
                };

            return View(viewModel);
        }

        // =====================================
        // 会社カレンダー
        // =====================================

        [HttpGet]
        public async Task<IActionResult> CompanyCalendar(
            string? yearMonth)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!TryParseYearMonth(
                    yearMonth,
                    out var targetMonth))
            {
                targetMonth =
                    new DateTime(
                        DateTime.Today.Year,
                        DateTime.Today.Month,
                        1
                    );

                TempData["ErrorMessage"] =
                    "対象年月の形式が正しくありません。";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            GenerateCompanyCalendar(
                string? yearMonth)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var adminId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Admin" ||
                adminId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!TryParseYearMonth(
                    yearMonth,
                    out var targetMonth))
            {
                TempData["ErrorMessage"] =
                    "対象年月の形式が正しくありません。";

                return RedirectToAction(
                    "CompanyCalendar"
                );
            }

            if (_monthlyClosingService.IsClosed(
                    targetMonth))
            {
                _operationLogService.Write(
                    actionName: "会社カレンダー作成",
                    targetType: "CompanyCalendarDay",
                    details:
                        $"月次締め済みのため、" +
                        $"会社カレンダーの作成を中止しました。" +
                        $"対象年月：{targetMonth:yyyy年MM月}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    $"{targetMonth:yyyy年MM月}は" +
                    "月次締め済みのため、" +
                    "会社カレンダーを作成できません。";

                return RedirectToAction(
                    "CompanyCalendar",
                    new
                    {
                        yearMonth =
                            targetMonth.ToString("yyyy-MM")
                    });
            }

            try
            {
                var createdCount =
                    await _companyCalendarService
                        .GenerateMonthAsync(
                            targetMonth.Year,
                            targetMonth.Month
                        );

                _operationLogService.Write(
                    actionName: "会社カレンダー作成",
                    targetType: "CompanyCalendarDay",
                    details:
                        $"会社カレンダーを作成しました。" +
                        $"対象年月：{targetMonth:yyyy年MM月}、" +
                        $"新規作成日数：{createdCount}日。",
                    result: "成功"
                );

                TempData["SuccessMessage"] =
                    createdCount > 0
                        ? $"{targetMonth:yyyy年MM月}の" +
                          $"会社カレンダーを作成しました。" +
                          $"（{createdCount}日）"
                        : $"{targetMonth:yyyy年MM月}の" +
                          "会社カレンダーはすでに作成済みです。";
            }
            catch (Exception exception)
            {
                _operationLogService.Write(
                    actionName: "会社カレンダー作成",
                    targetType: "CompanyCalendarDay",
                    details:
                        $"会社カレンダーの作成中に" +
                        $"エラーが発生しました。" +
                        $"対象年月：{targetMonth:yyyy年MM月}、" +
                        $"エラー：{exception.Message}",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "会社カレンダーの作成中に" +
                    "エラーが発生しました。";
            }

            return RedirectToAction(
                "CompanyCalendar",
                new
                {
                    yearMonth =
                        targetMonth.ToString("yyyy-MM")
                });
        }

        // =====================================
        // 会社カレンダー1日編集
        // =====================================

        [HttpGet]
        public async Task<IActionResult> EditCompanyCalendarDay(
            int id)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var calendarDay =
                await _context.CompanyCalendarDays
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.CompanyCalendarDayId == id
                    );

            if (calendarDay == null)
            {
                TempData["ErrorMessage"] =
                    "対象のカレンダー日が見つかりません。";

                return RedirectToAction(
                    "CompanyCalendar"
                );
            }

            var viewModel =
                new CompanyCalendarDayEditViewModel
                {
                    CompanyCalendarDayId =
                        calendarDay.CompanyCalendarDayId,

                    CalendarDate =
                        calendarDay.CalendarDate,

                    DayType =
                        calendarDay.DayType,

                    IsWorkingDay =
                        calendarDay.IsWorkingDay,

                    HolidayName =
                        calendarDay.HolidayName,

                    Note =
                        calendarDay.Note
                };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCompanyCalendarDay(
            CompanyCalendarDayEditViewModel viewModel)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var adminId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Admin" ||
                adminId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var calendarDay =
                await _context.CompanyCalendarDays
                    .FirstOrDefaultAsync(c =>
                        c.CompanyCalendarDayId ==
                        viewModel.CompanyCalendarDayId
                    );

            if (calendarDay == null)
            {
                _operationLogService.Write(
                    actionName: "会社カレンダー編集",
                    targetType: "CompanyCalendarDay",
                    targetId:
                        viewModel.CompanyCalendarDayId,
                    details:
                        "対象のカレンダー日が見つかりません。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象のカレンダー日が見つかりません。";

                return RedirectToAction(
                    "CompanyCalendar"
                );
            }

            // DBの日付を正しい値として使用する
            viewModel.CalendarDate =
                calendarDay.CalendarDate;

            var allowedDayTypes =
                new[]
                {
            "出勤日",
            "会社休日",
            "法定休日",
            "祝日",
            "特別出勤日",
            "年末年始休暇",
            "夏季休暇"
                };

            if (!allowedDayTypes.Contains(
                    viewModel.DayType))
            {
                ModelState.AddModelError(
                    nameof(viewModel.DayType),
                    "正しい日付区分を選択してください。"
                );
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            if (_monthlyClosingService.IsClosed(
                    calendarDay.CalendarDate))
            {
                _operationLogService.Write(
                    actionName: "会社カレンダー編集",
                    targetType: "CompanyCalendarDay",
                    targetId:
                        calendarDay.CompanyCalendarDayId,
                    details:
                        $"月次締め済みのため編集を拒否しました。" +
                        $"対象日：" +
                        $"{calendarDay.CalendarDate:yyyy/MM/dd}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    $"{calendarDay.CalendarDate:yyyy年MM月}" +
                    "は月次締め済みのため、編集できません。";

                return RedirectToAction(
                    "CompanyCalendar",
                    new
                    {
                        yearMonth =
                            calendarDay.CalendarDate
                                .ToString("yyyy-MM")
                    });
            }

            var beforeDayType =
                calendarDay.DayType;

            var beforeIsWorkingDay =
                calendarDay.IsWorkingDay;

            var beforeHolidayName =
                calendarDay.HolidayName;

            var beforeNote =
                calendarDay.Note;

            var normalizedHolidayName =
                string.IsNullOrWhiteSpace(
                    viewModel.HolidayName)
                    ? null
                    : viewModel.HolidayName.Trim();

            var normalizedNote =
                string.IsNullOrWhiteSpace(
                    viewModel.Note)
                    ? null
                    : viewModel.Note.Trim();

            // 日付区分から勤務日・休日を自動決定する
            var isWorkingDay =
                viewModel.DayType == "出勤日" ||
                viewModel.DayType == "特別出勤日";

            calendarDay.DayType =
                viewModel.DayType;

            calendarDay.IsWorkingDay =
                isWorkingDay;

            calendarDay.HolidayName =
                normalizedHolidayName;

            calendarDay.Note =
                normalizedNote;

            calendarDay.UpdatedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            var beforeWorkingDayText =
                beforeIsWorkingDay
                    ? "所定労働日"
                    : "休日";

            var afterWorkingDayText =
                calendarDay.IsWorkingDay
                    ? "所定労働日"
                    : "休日";

            _operationLogService.Write(
                actionName: "会社カレンダー編集",
                targetType: "CompanyCalendarDay",
                targetId:
                    calendarDay.CompanyCalendarDayId,
                details:
                    $"会社カレンダーを更新しました。" +
                    $"対象日：" +
                    $"{calendarDay.CalendarDate:yyyy/MM/dd}、" +
                    $"日付区分：" +
                    $"{beforeDayType} → {calendarDay.DayType}、" +
                    $"勤務区分：" +
                    $"{beforeWorkingDayText} → " +
                    $"{afterWorkingDayText}、" +
                    $"休日・行事名：" +
                    $"{beforeHolidayName ?? "-"} → " +
                    $"{calendarDay.HolidayName ?? "-"}、" +
                    $"備考：" +
                    $"{beforeNote ?? "-"} → " +
                    $"{calendarDay.Note ?? "-"}。",
                result: "成功"
            );

            TempData["SuccessMessage"] =
                $"{calendarDay.CalendarDate:yyyy/MM/dd}の" +
                "会社カレンダーを更新しました。";

            return RedirectToAction(
                "CompanyCalendar",
                new
                {
                    yearMonth =
                        calendarDay.CalendarDate
                            .ToString("yyyy-MM")
                });
        }

        public IActionResult Employees(
            int? departmentId,
            string? keyword)
        {
            var role = HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Employees
                .Include(e => e.Department)
                .AsQueryable();

            if (departmentId.HasValue &&
                departmentId.Value > 0)
            {
                query = query.Where(e =>
                    e.DepartmentId == departmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var searchKeyword = keyword.Trim();

                query = query.Where(e =>
                    e.Name.Contains(searchKeyword) ||
                    e.Email.Contains(searchKeyword));
            }

            var viewModel = new EmployeeListViewModel
            {
                DepartmentId = departmentId,
                Keyword = keyword,
                Departments = GetDepartments(),

                Employees = query
                    .OrderBy(e => e.EmployeeId)
                    .AsEnumerable()
                    .Select(e => new EmployeeListItemViewModel
                    {
                        EmployeeId = e.EmployeeId,
                        Name = e.Name,
                        Email = e.Email,

                        DepartmentName =
                            e.Department != null
                                ? e.Department.DepartmentName
                                : "",

                        Role =
                          e.Role,

                        IsActive =
                         e.IsActive,

                        // 勤務条件
                        ScheduledStartTime =
                         e.ScheduledStartTime,

                        ScheduledEndTime =
                          e.ScheduledEndTime,

                        ScheduledWorkMinutes =
                                  e.ScheduledWorkMinutes,

                        // 初回パスワード変更状態
                        MustChangePassword =
                         e.MustChangePassword,

                        FailedLoginCount =
                         e.FailedLoginCount,

                        IsLocked =
                            e.LockoutEndAt.HasValue &&
                            e.LockoutEndAt.Value > DateTime.Now,

                        RemainingLockMinutes =
                            e.LockoutEndAt.HasValue &&
                            e.LockoutEndAt.Value > DateTime.Now
                                ? Math.Max(
                                    1,
                                    (int)Math.Ceiling(
                                        (e.LockoutEndAt.Value -
                                         DateTime.Now).TotalMinutes))
                                : 0
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult CreateEmployee()
        {
            var role = HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new EmployeeCreateViewModel
            {
                Departments = GetDepartments()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateEmployee(
            EmployeeCreateViewModel viewModel)
        {
            var role = HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            viewModel.Departments = GetDepartments();

            if (!ModelState.IsValid)
            {
                _operationLogService.Write(
                    actionName: "社員登録",
                    targetType: "Employee",
                    details:
                        $"入力内容にエラーがあります。" +
                        $"氏名：{viewModel.Name}、" +
                        $"メールアドレス：{viewModel.Email}",
                    result: "失敗"
                );

                return View(viewModel);
            }

            var normalizedName = viewModel.Name.Trim();
            var normalizedEmail = viewModel.Email.Trim();

            var emailExists = _context.Employees
                .Any(e => e.Email == normalizedEmail);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "このメールアドレスはすでに使用されています。"
                );

                _operationLogService.Write(
                    actionName: "社員登録",
                    targetType: "Employee",
                    details:
                        $"既に使用されているメールアドレスのため、" +
                        $"社員登録を拒否しました。" +
                        $"メールアドレス：{normalizedEmail}",
                    result: "失敗"
                );

                return View(viewModel);
            }

            var now = DateTime.Now;

            var employee = new Employee
            {
                Name = normalizedName,
                Email = normalizedEmail,

                PasswordHash =
         PasswordHelper.HashPassword(
             viewModel.Password
         ),

                DepartmentId =
         viewModel.DepartmentId,

                Role =
         viewModel.Role,

                JoinDate =
         viewModel.JoinDate.Date,

                IsActive = true,

                // 勤務時間
                ScheduledStartTime =
         viewModel.ScheduledStartTime,

                ScheduledEndTime =
         viewModel.ScheduledEndTime,

                ScheduledWorkMinutes =
         viewModel.ScheduledWorkMinutes,

                // 昼休憩
                LunchBreakStartTime =
         viewModel.LunchBreakStartTime,

                LunchBreakEndTime =
         viewModel.LunchBreakEndTime,

                // 小休憩1
                SmallBreak1StartTime =
         viewModel.SmallBreak1StartTime,

                SmallBreak1EndTime =
         viewModel.SmallBreak1EndTime,

                // 小休憩2
                SmallBreak2StartTime =
         viewModel.SmallBreak2StartTime,

                SmallBreak2EndTime =
         viewModel.SmallBreak2EndTime,

                // 初回ログイン時のパスワード変更
                MustChangePassword = true,

                FailedLoginCount = 0,
                LastFailedLoginAt = null,
                LockoutEndAt = null,

                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Employees.Add(employee);
            _context.SaveChanges();

            var balance = new PaidLeaveBalance
            {
                EmployeeId = employee.EmployeeId,
                Year = DateTime.Today.Year,
                GrantedDays = 0.0,
                UsedDays = 0.0,
                RemainingDays = 0.0,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.PaidLeaveBalances.Add(balance);
            _context.SaveChanges();

            _operationLogService.Write(
                actionName: "社員登録",
                targetType: "Employee",
                targetId: employee.EmployeeId,
                details:
                    $"社員を登録しました。" +
                    $"氏名：{employee.Name}、" +
                    $"メールアドレス：{employee.Email}、" +
                    $"権限：{employee.Role}、" +
                    $"入社日：{employee.JoinDate:yyyy/MM/dd}",
                result: "成功"
            );

            TempData["SuccessMessage"] =
                $"{employee.Name}さんを登録しました。";

            return RedirectToAction("Employees");
        }

        [HttpGet]
        public IActionResult EditEmployee(int id)
        {
            var role = HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = _context.Employees
                .FirstOrDefault(e => e.EmployeeId == id);

            if (employee == null)
            {
                TempData["ErrorMessage"] =
                    "対象の社員が見つかりません。";

                return RedirectToAction("Employees");
            }

            var viewModel = new EmployeeEditViewModel
            {
                EmployeeId =
        employee.EmployeeId,

                Name =
        employee.Name,

                Email =
        employee.Email,

                DepartmentId =
        employee.DepartmentId,

                Role =
        employee.Role,

                JoinDate =
        employee.JoinDate,

                IsActive =
        employee.IsActive,

                // 勤務時間
                ScheduledStartTime =
        employee.ScheduledStartTime,

                ScheduledEndTime =
        employee.ScheduledEndTime,

                ScheduledWorkHours =
        employee.ScheduledWorkMinutes / 60,

                ScheduledWorkMinutePart =
        employee.ScheduledWorkMinutes % 60,

                // 昼休憩
                LunchBreakStartTime =
        employee.LunchBreakStartTime,

                LunchBreakEndTime =
        employee.LunchBreakEndTime,

                // 小休憩1
                SmallBreak1StartTime =
        employee.SmallBreak1StartTime,

                SmallBreak1EndTime =
        employee.SmallBreak1EndTime,

                // 小休憩2
                SmallBreak2StartTime =
        employee.SmallBreak2StartTime,

                SmallBreak2EndTime =
        employee.SmallBreak2EndTime,

                Departments =
        GetDepartments()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEmployee(
            EmployeeEditViewModel viewModel)
        {
            var role = HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            viewModel.Departments = GetDepartments();

            if (!ModelState.IsValid)
            {
                _operationLogService.Write(
                    actionName: "社員編集",
                    targetType: "Employee",
                    targetId: viewModel.EmployeeId,
                    details:
                        $"入力内容にエラーがあります。" +
                        $"社員ID：{viewModel.EmployeeId}。",
                    result: "失敗"
                );

                return View(viewModel);
            }

            var employee = _context.Employees
                .FirstOrDefault(e =>
                    e.EmployeeId == viewModel.EmployeeId);

            if (employee == null)
            {
                _operationLogService.Write(
                    actionName: "社員編集",
                    targetType: "Employee",
                    targetId: viewModel.EmployeeId,
                    details:
                        $"対象の社員が見つかりません。" +
                        $"社員ID：{viewModel.EmployeeId}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象の社員が見つかりません。";

                return RedirectToAction("Employees");
            }

            var normalizedName = viewModel.Name.Trim();
            var normalizedEmail = viewModel.Email.Trim();

            var emailExists = _context.Employees
                .Any(e =>
                    e.Email == normalizedEmail &&
                    e.EmployeeId != viewModel.EmployeeId);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "このメールアドレスはすでに使用されています。"
                );

                _operationLogService.Write(
                    actionName: "社員編集",
                    targetType: "Employee",
                    targetId: employee.EmployeeId,
                    details:
                        $"既に使用されているメールアドレスへの変更を" +
                        $"拒否しました。" +
                        $"メールアドレス：{normalizedEmail}。",
                    result: "失敗"
                );

                return View(viewModel);
            }

            var beforeName =
     employee.Name;

            var beforeEmail =
                employee.Email;

            var beforeDepartmentId =
                employee.DepartmentId;

            var beforeRole =
                employee.Role;

            var beforeJoinDate =
                employee.JoinDate;

            var beforeIsActive =
                employee.IsActive;

            // 変更前の勤務時間
            var beforeScheduledStartTime =
                employee.ScheduledStartTime;

            var beforeScheduledEndTime =
                employee.ScheduledEndTime;

            var beforeScheduledWorkMinutes =
                employee.ScheduledWorkMinutes;

            var beforeLunchBreakStartTime =
                employee.LunchBreakStartTime;

            var beforeLunchBreakEndTime =
                employee.LunchBreakEndTime;

            var beforeSmallBreak1StartTime =
                employee.SmallBreak1StartTime;

            var beforeSmallBreak1EndTime =
                employee.SmallBreak1EndTime;

            var beforeSmallBreak2StartTime =
                employee.SmallBreak2StartTime;

            var beforeSmallBreak2EndTime =
                employee.SmallBreak2EndTime;

            // 基本情報
            employee.Name =
                normalizedName;

            employee.Email =
                normalizedEmail;

            employee.DepartmentId =
                viewModel.DepartmentId;

            employee.Role =
                viewModel.Role;

            employee.JoinDate =
                viewModel.JoinDate.Date;

            employee.IsActive =
                viewModel.IsActive;

            // 勤務時間
            employee.ScheduledStartTime =
                viewModel.ScheduledStartTime;

            employee.ScheduledEndTime =
                viewModel.ScheduledEndTime;

            employee.ScheduledWorkMinutes =
                viewModel.ScheduledWorkMinutes;

            // 昼休憩
            employee.LunchBreakStartTime =
                viewModel.LunchBreakStartTime;

            employee.LunchBreakEndTime =
                viewModel.LunchBreakEndTime;

            // 小休憩1
            employee.SmallBreak1StartTime =
                viewModel.SmallBreak1StartTime;

            employee.SmallBreak1EndTime =
                viewModel.SmallBreak1EndTime;

            // 小休憩2
            employee.SmallBreak2StartTime =
                viewModel.SmallBreak2StartTime;

            employee.SmallBreak2EndTime =
                viewModel.SmallBreak2EndTime;

            employee.UpdatedAt =
                DateTime.Now;

            _context.SaveChanges();

            var beforeActiveText =
                beforeIsActive
                    ? "有効"
                    : "無効";

            var afterActiveText =
                employee.IsActive
                    ? "有効"
                    : "無効";

            var beforeSmallBreak1Text =
                beforeSmallBreak1StartTime.HasValue &&
                beforeSmallBreak1EndTime.HasValue
                    ? $"{beforeSmallBreak1StartTime.Value:hh\\:mm}" +
                      $"～{beforeSmallBreak1EndTime.Value:hh\\:mm}"
                    : "なし";

            var afterSmallBreak1Text =
                employee.SmallBreak1StartTime.HasValue &&
                employee.SmallBreak1EndTime.HasValue
                    ? $"{employee.SmallBreak1StartTime.Value:hh\\:mm}" +
                      $"～{employee.SmallBreak1EndTime.Value:hh\\:mm}"
                    : "なし";

            var beforeSmallBreak2Text =
                beforeSmallBreak2StartTime.HasValue &&
                beforeSmallBreak2EndTime.HasValue
                    ? $"{beforeSmallBreak2StartTime.Value:hh\\:mm}" +
                      $"～{beforeSmallBreak2EndTime.Value:hh\\:mm}"
                    : "なし";

            var afterSmallBreak2Text =
                employee.SmallBreak2StartTime.HasValue &&
                employee.SmallBreak2EndTime.HasValue
                    ? $"{employee.SmallBreak2StartTime.Value:hh\\:mm}" +
                      $"～{employee.SmallBreak2EndTime.Value:hh\\:mm}"
                    : "なし";

            _operationLogService.Write(
                actionName: "社員編集",
                targetType: "Employee",
                targetId: employee.EmployeeId,
                details:
                    $"社員情報を更新しました。" +
                    $"氏名：{beforeName} → {employee.Name}、" +
                    $"メール：{beforeEmail} → {employee.Email}、" +
                    $"部署ID：{beforeDepartmentId} → " +
                    $"{employee.DepartmentId}、" +
                    $"権限：{beforeRole} → {employee.Role}、" +
                    $"入社日：{beforeJoinDate:yyyy/MM/dd} → " +
                    $"{employee.JoinDate:yyyy/MM/dd}、" +
                    $"状態：{beforeActiveText} → {afterActiveText}、" +
                    $"勤務時間：" +
                    $"{beforeScheduledStartTime:hh\\:mm}～" +
                    $"{beforeScheduledEndTime:hh\\:mm} → " +
                    $"{employee.ScheduledStartTime:hh\\:mm}～" +
                    $"{employee.ScheduledEndTime:hh\\:mm}、" +
                    $"所定労働時間：" +
                    $"{beforeScheduledWorkMinutes / 60}時間" +
                    $"{beforeScheduledWorkMinutes % 60:D2}分 → " +
                    $"{employee.ScheduledWorkMinutes / 60}時間" +
                    $"{employee.ScheduledWorkMinutes % 60:D2}分、" +
                    $"昼休憩：" +
                    $"{beforeLunchBreakStartTime:hh\\:mm}～" +
                    $"{beforeLunchBreakEndTime:hh\\:mm} → " +
                    $"{employee.LunchBreakStartTime:hh\\:mm}～" +
                    $"{employee.LunchBreakEndTime:hh\\:mm}、" +
                    $"小休憩1：{beforeSmallBreak1Text} → " +
                    $"{afterSmallBreak1Text}、" +
                    $"小休憩2：{beforeSmallBreak2Text} → " +
                    $"{afterSmallBreak2Text}。",
                result: "成功"
            );

            TempData["SuccessMessage"] =
                $"{employee.Name}さんの社員情報を更新しました。";

            return RedirectToAction("Employees");
        }

        public async Task<IActionResult> AttendanceList(
string? yearMonth,
int? departmentId,
string? keyword)
        {
            var role =
                HttpContext.Session.GetString(
                    "LoginUserRole"
                );

            if (role != "Admin")
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var targetMonth =
                DateTime.Today;

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
                        parsedMonth;
                }
            }

            var startDate =
                new DateTime(
                    targetMonth.Year,
                    targetMonth.Month,
                    1
                );

            var endDate =
                startDate.AddMonths(1);

            var today =
                DateTime.Today;

            // =====================================
            // 対象社員
            // =====================================

            var employeeQuery =
                _context.Employees
                    .AsNoTracking()
                    .Include(employee =>
                        employee.Department)
                    .AsQueryable();

            if (departmentId.HasValue &&
                departmentId.Value > 0)
            {
                employeeQuery =
                    employeeQuery.Where(employee =>
                        employee.DepartmentId ==
                            departmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                    keyword))
            {
                var searchKeyword =
                    keyword.Trim();

                employeeQuery =
                    employeeQuery.Where(employee =>
                        employee.Name.Contains(
                            searchKeyword
                        ) ||
                        employee.Email.Contains(
                            searchKeyword
                        ));
            }

            var employees =
                await employeeQuery
                    .OrderBy(employee =>
                        employee.EmployeeId)
                    .ToListAsync();

            var employeeIds =
                employees
                    .Select(employee =>
                        employee.EmployeeId)
                    .ToList();

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

            // 今日までの所定労働日を作成する
            var workingDates =
                new List<DateTime>();

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
                    workingDates.Add(
                        currentDate
                    );
                }

                currentDate =
                    currentDate.AddDays(1);
            }

            // =====================================
            // 勤怠データ
            // =====================================

            var attendanceRows =
                await _context.Attendances
                    .AsNoTracking()
                    .Where(attendance =>
                        employeeIds.Contains(
                            attendance.EmployeeId
                        ) &&
                        attendance.AttendanceDate >=
                            startDate &&
                        attendance.AttendanceDate <
                            endDate)
                    .ToListAsync();

            var attendanceLookup =
                attendanceRows
                    .GroupBy(attendance =>
                        (
                            attendance.EmployeeId,
                            AttendanceDate:
                                attendance
                                    .AttendanceDate
                                    .Date
                        ))
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
                        employeeIds.Contains(
                            request.EmployeeId
                        ) &&
                        request.Status ==
                            "承認" &&
                        request.Days >
                            0 &&
                        request.LeaveDate >=
                            startDate &&
                        request.LeaveDate <
                            endDate)
                    .ToListAsync();

            var approvedPaidLeaveLookup =
                approvedPaidLeaveRows
                    .GroupBy(request =>
                        (
                            request.EmployeeId,
                            LeaveDate:
                                request
                                    .LeaveDate
                                    .Date
                        ))
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.Sum(request =>
                                request.Days)
                    );

            // =====================================
            // 申請中の勤怠修正申請
            // =====================================

            var pendingCorrectionRequestKeys =
                await _context
                    .AttendanceCorrectionRequests
                    .AsNoTracking()
                    .Where(request =>
                        employeeIds.Contains(
                            request.EmployeeId
                        ) &&
                        request.TargetDate >=
                            startDate &&
                        request.TargetDate <
                            endDate &&
                        request.Status ==
                            "申請中")
                    .Select(request =>
                        new
                        {
                            request.EmployeeId,
                            TargetDate =
                                request
                                    .TargetDate
                                    .Date
                        })
                    .Distinct()
                    .ToListAsync();

            var pendingCorrectionRequestLookup =
                pendingCorrectionRequestKeys
                    .Select(request =>
                        (
                            request.EmployeeId,
                            request.TargetDate
                        ))
                    .ToHashSet();

            // =====================================
            // 欠勤確定者
            // =====================================

            var confirmerIds =
                attendanceRows
                    .Where(attendance =>
                        attendance
                            .AbsenceConfirmedBy
                            .HasValue)
                    .Select(attendance =>
                        attendance
                            .AbsenceConfirmedBy!
                            .Value)
                    .Distinct()
                    .ToList();

            var confirmerNames =
                await _context.Employees
                    .AsNoTracking()
                    .Where(employee =>
                        confirmerIds.Contains(
                            employee.EmployeeId
                        ))
                    .ToDictionaryAsync(
                        employee =>
                            employee.EmployeeId,
                        employee =>
                            employee.Name
                    );

            // =====================================
            // 社員×日付の一覧を作成
            // =====================================

            var attendanceItems =
                new List<
                    AdminAttendanceListItemViewModel>();

            foreach (var employee
                     in employees)
            {
                var employeeDates =
                    workingDates
                        .ToHashSet();

                // 休日出勤など、実際の勤怠がある日も表示
                foreach (var attendanceDate
                         in attendanceRows
                             .Where(attendance =>
                                 attendance.EmployeeId ==
                                     employee.EmployeeId)
                             .Select(attendance =>
                                 attendance
                                     .AttendanceDate
                                     .Date))
                {
                    employeeDates.Add(
                        attendanceDate
                    );
                }

                // 将来の承認済み有給予定日も表示
                foreach (var paidLeaveDate
                         in approvedPaidLeaveRows
                             .Where(request =>
                                 request.EmployeeId ==
                                     employee.EmployeeId)
                             .Select(request =>
                                 request
                                     .LeaveDate
                                     .Date))
                {
                    employeeDates.Add(
                        paidLeaveDate
                    );
                }

                foreach (var attendanceDate
                         in employeeDates)
                {
                    attendanceLookup.TryGetValue(
                        (
                            employee.EmployeeId,
                            attendanceDate
                        ),
                        out var attendance
                    );

                    var hasApprovedPaidLeave =
                        approvedPaidLeaveLookup
                            .ContainsKey(
                                (
                                    employee.EmployeeId,
                                    attendanceDate
                                )
                            );

                    var hasPendingCorrectionRequest =
                        pendingCorrectionRequestLookup
                            .Contains(
                                (
                                    employee.EmployeeId,
                                    attendanceDate
                                )
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

                    attendanceItems.Add(
                        new AdminAttendanceListItemViewModel
                        {
                            AttendanceId =
                                attendance
                                    ?.AttendanceId,

                            EmployeeId =
                                employee.EmployeeId,

                            AttendanceDate =
                                attendanceDate,

                            EmployeeName =
                                employee.Name,

                            DepartmentName =
                                employee.Department
                                    ?.DepartmentName ??
                                string.Empty,

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

                            HasPendingCorrectionRequest =
                                hasPendingCorrectionRequest,

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
            }

            var viewModel =
                new AdminAttendanceListViewModel
                {
                    YearMonth =
                        startDate.ToString(
                            "yyyy-MM"
                        ),

                    DepartmentId =
                        departmentId,

                    Keyword =
                        keyword,

                    Departments =
                        GetDepartments(),

                    Attendances =
                        attendanceItems
                            .OrderByDescending(item =>
                                item.AttendanceDate)
                            .ThenBy(item =>
                                item.EmployeeId)
                            .ToList()
                };

            return View(
                viewModel
            );
        }
        // =====================================
        // 欠勤確定
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAbsence(
                  int employeeId,
                  DateTime attendanceDate,
                  string? absenceReason,
                  string? yearMonth,
                  int? departmentId,
                  string? keyword)
        {
            var role =
                HttpContext.Session.GetString(
                    "LoginUserRole"
                );

            var adminId =
                HttpContext.Session.GetInt32(
                    "LoginUserId"
                );

            if (role != "Admin" ||
                adminId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            attendanceDate =
                attendanceDate.Date;

            var redirectYearMonth =
                string.IsNullOrWhiteSpace(
                    yearMonth)
                    ? attendanceDate.ToString(
                        "yyyy-MM"
                    )
                    : yearMonth;

            string RedirectActionName()
            {
                return nameof(
                    AttendanceList
                );
            }

            IActionResult RedirectToList()
            {
                return RedirectToAction(
                    RedirectActionName(),
                    new
                    {
                        yearMonth =
                            redirectYearMonth,

                        departmentId,

                        keyword
                    }
                );
            }

            // =====================================
            // 基本入力チェック
            // =====================================

            if (attendanceDate >
                DateTime.Today)
            {
                TempData["ErrorMessage"] =
                    "未来の日付を欠勤として確定することはできません。";

                return RedirectToList();
            }

            var normalizedReason =
                absenceReason?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(
                    normalizedReason))
            {
                TempData["ErrorMessage"] =
                    "欠勤理由を入力してください。";

                return RedirectToList();
            }

            if (normalizedReason.Length >
                300)
            {
                TempData["ErrorMessage"] =
                    "欠勤理由は300文字以内で入力してください。";

                return RedirectToList();
            }

            // =====================================
            // 月次締め済みチェック
            // =====================================

            if (_monthlyClosingService.IsClosed(
                    attendanceDate))
            {
                TempData["ErrorMessage"] =
                    $"{attendanceDate:yyyy年MM月}は" +
                    "月次締め済みのため、" +
                    "欠勤を確定できません。";

                _operationLogService.Write(
                    actionName:
                        "欠勤確定",

                    targetType:
                        "MonthlyClosing",

                    details:
                        $"月次締め済みのため、" +
                        $"欠勤確定を拒否しました。" +
                        $"社員ID：{employeeId}、" +
                        $"対象日：{attendanceDate:yyyy/MM/dd}。",

                    result:
                        "失敗"
                );

                return RedirectToList();
            }

            // =====================================
            // 社員確認
            // =====================================

            var employee =
                await _context.Employees
                    .FirstOrDefaultAsync(
                        employee =>
                            employee.EmployeeId ==
                                employeeId &&
                            employee.IsActive
                    );

            if (employee == null)
            {
                TempData["ErrorMessage"] =
                    "対象社員が見つかりません。";

                return RedirectToList();
            }

            // =====================================
            // 会社カレンダー確認
            // =====================================

            var calendarDay =
                await _context.CompanyCalendarDays
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        day =>
                            day.CalendarDate ==
                                attendanceDate
                    );

            var isCompanyWorkingDay =
                calendarDay != null
                    ? calendarDay.IsWorkingDay
                    : attendanceDate.DayOfWeek !=
                        DayOfWeek.Saturday &&
                      attendanceDate.DayOfWeek !=
                        DayOfWeek.Sunday;

            if (!isCompanyWorkingDay)
            {
                TempData["ErrorMessage"] =
                    "会社休日は欠勤として確定できません。";

                return RedirectToList();
            }

            // =====================================
            // 承認済み有給確認
            // =====================================

            var hasApprovedPaidLeave =
                await _context.PaidLeaveRequests
                    .AsNoTracking()
                    .AnyAsync(
                        request =>
                            request.EmployeeId ==
                                employeeId &&
                            request.LeaveDate ==
                                attendanceDate &&
                            request.Status ==
                                "承認"
                    );

            if (hasApprovedPaidLeave)
            {
                TempData["ErrorMessage"] =
                    "承認済み有給の日は欠勤として確定できません。";

                return RedirectToList();
            }

            // =====================================
            // 申請中の勤怠修正確認
            // =====================================

            var pendingCorrectionRequest =
                await _context
                    .AttendanceCorrectionRequests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        request =>
                            request.EmployeeId ==
                                employeeId &&
                            request.TargetDate ==
                                attendanceDate &&
                            request.Status ==
                                "申請中"
                    );

            if (pendingCorrectionRequest != null)
            {
                TempData["ErrorMessage"] =
                    "申請中の勤怠修正申請があるため、" +
                    "欠勤を確定できません。" +
                    "先に勤怠修正申請を承認または却下してください。";

                _operationLogService.Write(
                    actionName:
                        "欠勤確定",

                    targetType:
                        "AttendanceCorrectionRequest",

                    targetId:
                        pendingCorrectionRequest.RequestId,

                    details:
                        $"申請中の勤怠修正申請があるため、" +
                        $"欠勤確定を拒否しました。" +
                        $"社員：{employee.Name}、" +
                        $"対象日：{attendanceDate:yyyy/MM/dd}、" +
                        $"修正項目：" +
                        $"{pendingCorrectionRequest.CorrectionType}。",

                    result:
                        "失敗"
                );

                return RedirectToList();
            }

            // =====================================
            // 勤怠確認
            // =====================================

            var attendance =
                await _context.Attendances
                    .FirstOrDefaultAsync(
                        item =>
                            item.EmployeeId ==
                                employeeId &&
                            item.AttendanceDate ==
                                attendanceDate
                    );

            if (attendance
                    ?.ClockInTime
                    .HasValue ==
                true)
            {
                TempData["ErrorMessage"] =
                    "出勤打刻がある日は欠勤として確定できません。";

                return RedirectToList();
            }

            if (attendance?.IsAbsent ==
                true)
            {
                TempData["ErrorMessage"] =
                    "この日はすでに欠勤確定済みです。";

                return RedirectToList();
            }

            // =====================================
            // 欠勤確定保存
            // =====================================

            var now =
                DateTime.Now;

            if (attendance == null)
            {
                attendance =
                    new Attendance
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        AttendanceDate =
                            attendanceDate,

                        ClockInTime =
                            null,

                        ClockOutTime =
                            null,

                        BreakMinutes =
                            0,

                        WorkMinutes =
                            0,

                        LateMinutes =
                            0,

                        OvertimeMinutes =
                            0,

                        ScheduledStartTimeSnapshot =
                            employee.ScheduledStartTime,

                        ScheduledEndTimeSnapshot =
                            employee.ScheduledEndTime,

                        ScheduledWorkMinutesSnapshot =
                            employee.ScheduledWorkMinutes,

                        LunchBreakStartTimeSnapshot =
                            employee.LunchBreakStartTime,

                        LunchBreakEndTimeSnapshot =
                            employee.LunchBreakEndTime,

                        SmallBreak1StartTimeSnapshot =
                            employee.SmallBreak1StartTime,

                        SmallBreak1EndTimeSnapshot =
                            employee.SmallBreak1EndTime,

                        SmallBreak2StartTimeSnapshot =
                            employee.SmallBreak2StartTime,

                        SmallBreak2EndTimeSnapshot =
                            employee.SmallBreak2EndTime,

                        CreatedAt =
                            now
                    };

                _context.Attendances.Add(
                    attendance
                );
            }

            attendance.Status =
                "欠勤";

            attendance.IsAbsent =
                true;

            attendance.AbsenceReason =
                normalizedReason;

            attendance.AbsenceConfirmedAt =
                now;

            attendance.AbsenceConfirmedBy =
                adminId.Value;

            attendance.ClockInTime =
                null;

            attendance.ClockOutTime =
                null;

            attendance.BreakMinutes =
                0;

            attendance.WorkMinutes =
                0;

            attendance.LateMinutes =
                0;

            attendance.OvertimeMinutes =
                0;

            attendance.UpdatedAt =
                now;

            await _context.SaveChangesAsync();

            _operationLogService.Write(
                actionName:
                    "欠勤確定",

                targetType:
                    "Attendance",

                targetId:
                    attendance.AttendanceId,

                details:
                    $"欠勤を確定しました。" +
                    $"社員：{employee.Name}、" +
                    $"対象日：{attendanceDate:yyyy/MM/dd}、" +
                    $"理由：{normalizedReason}。",

                result:
                    "成功"
            );

            TempData["SuccessMessage"] =
                $"{employee.Name}の" +
                $"{attendanceDate:yyyy/MM/dd}を" +
                "欠勤として確定しました。";

            return RedirectToList();
        }


        // =====================================
        // 欠勤確定取消
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAbsence(
            int attendanceId,
            string? yearMonth,
            int? departmentId,
            string? keyword)
        {
            var role =
                HttpContext.Session.GetString(
                    "LoginUserRole"
                );

            var adminId =
                HttpContext.Session.GetInt32(
                    "LoginUserId"
                );

            if (role != "Admin" ||
                adminId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var attendance =
                await _context.Attendances
                    .Include(item =>
                        item.Employee)
                    .FirstOrDefaultAsync(
                        item =>
                            item.AttendanceId ==
                                attendanceId
                    );

            var redirectYearMonth =
                !string.IsNullOrWhiteSpace(
                    yearMonth)
                    ? yearMonth
                    : attendance
                        ?.AttendanceDate
                        .ToString("yyyy-MM") ??
                      DateTime.Today.ToString(
                          "yyyy-MM"
                      );

            IActionResult RedirectToList()
            {
                return RedirectToAction(
                    nameof(
                        AttendanceList
                    ),
                    new
                    {
                        yearMonth =
                            redirectYearMonth,

                        departmentId,

                        keyword
                    }
                );
            }

            if (attendance == null)
            {
                TempData["ErrorMessage"] =
                    "対象の勤怠データが見つかりません。";

                return RedirectToList();
            }

            if (_monthlyClosingService.IsClosed(
                    attendance.AttendanceDate))
            {
                TempData["ErrorMessage"] =
                    $"{attendance.AttendanceDate:yyyy年MM月}は" +
                    "月次締め済みのため、" +
                    "欠勤確定を取り消せません。";

                _operationLogService.Write(
                    actionName:
                        "欠勤確定取消",

                    targetType:
                        "MonthlyClosing",

                    details:
                        $"月次締め済みのため、" +
                        $"欠勤確定取消を拒否しました。" +
                        $"勤怠ID：{attendance.AttendanceId}、" +
                        $"対象日：" +
                        $"{attendance.AttendanceDate:yyyy/MM/dd}。",

                    result:
                        "失敗"
                );

                return RedirectToList();
            }

            if (!attendance.IsAbsent)
            {
                TempData["ErrorMessage"] =
                    "この勤怠は欠勤確定されていません。";

                return RedirectToList();
            }

            var employeeName =
                attendance.Employee
                    ?.Name ??
                $"社員ID：{attendance.EmployeeId}";

            var previousReason =
                attendance.AbsenceReason ??
                string.Empty;

            attendance.IsAbsent =
                false;

            attendance.Status =
                "未出勤";

            attendance.AbsenceReason =
                null;

            attendance.AbsenceConfirmedAt =
                null;

            attendance.AbsenceConfirmedBy =
                null;

            attendance.UpdatedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            _operationLogService.Write(
                actionName:
                    "欠勤確定取消",

                targetType:
                    "Attendance",

                targetId:
                    attendance.AttendanceId,

                details:
                    $"欠勤確定を取り消しました。" +
                    $"社員：{employeeName}、" +
                    $"対象日：" +
                    $"{attendance.AttendanceDate:yyyy/MM/dd}、" +
                    $"取消前理由：" +
                    $"{previousReason}。",

                result:
                    "成功"
            );

            TempData["SuccessMessage"] =
                $"{employeeName}の" +
                $"{attendance.AttendanceDate:yyyy/MM/dd}の" +
                "欠勤確定を取り消しました。";

            return RedirectToList();
        }

        public async Task<IActionResult> MonthlySummary(
 string? yearMonth,
 int? departmentId)
        {
            var role =
                HttpContext.Session.GetString(
                    "LoginUserRole"
                );

            if (role != "Admin")
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var targetMonth =
                DateTime.Today;

            if (!string.IsNullOrWhiteSpace(
                    yearMonth))
            {
                if (DateTime.TryParse(
                        yearMonth + "-01",
                        out var parsedMonth))
                {
                    targetMonth =
                        parsedMonth;
                }
            }

            var startDate =
                new DateTime(
                    targetMonth.Year,
                    targetMonth.Month,
                    1
                );

            var endDate =
                startDate.AddMonths(1);

            var today =
                DateTime.Today;

            // =====================================
            // 会社カレンダー
            // =====================================

            var companyCalendarDays =
                await _companyCalendarService
                    .GetMonthAsync(
                        startDate.Year,
                        startDate.Month
                    );

            var expectedCalendarDayCount =
                DateTime.DaysInMonth(
                    startDate.Year,
                    startDate.Month
                );

            var isCompanyCalendarGenerated =
                companyCalendarDays.Count ==
                expectedCalendarDayCount;

            var companyWorkingDates =
                companyCalendarDays
                    .Where(c =>
                        c.IsWorkingDay)
                    .Select(c =>
                        c.CalendarDate.Date)
                    .ToHashSet();

            var companyHolidayDates =
                companyCalendarDays
                    .Where(c =>
                        !c.IsWorkingDay)
                    .Select(c =>
                        c.CalendarDate.Date)
                    .ToHashSet();

            // 未打刻は完了した日だけ判定する。
            // 当日は勤務途中の可能性があるため除外する。
            var missingStampCheckEndDate =
                endDate <= today
                    ? endDate
                    : today;

            // =====================================
            // 対象社員
            // =====================================

            var employeesQuery =
                _context.Employees
                    .AsNoTracking()
                    .Include(e =>
                        e.Department)
                    .Where(e =>
                        e.IsActive &&
                        e.Role == "Employee" &&
                        e.JoinDate < endDate)
                    .AsQueryable();

            if (departmentId.HasValue &&
                departmentId.Value > 0)
            {
                employeesQuery =
                    employeesQuery.Where(e =>
                        e.DepartmentId ==
                        departmentId.Value
                    );
            }

            var employees =
                await employeesQuery
                    .OrderBy(e =>
                        e.EmployeeId)
                    .ToListAsync();

            var employeeIds =
                employees
                    .Select(e =>
                        e.EmployeeId)
                    .ToList();

            // =====================================
            // 勤怠データ
            // =====================================

            var attendances =
                await _context.Attendances
                    .AsNoTracking()
                    .Where(a =>
                        employeeIds.Contains(
                            a.EmployeeId
                        ) &&
                        a.AttendanceDate >=
                            startDate &&
                        a.AttendanceDate <
                            endDate)
                    .ToListAsync();

            // =====================================
            // 承認済み有給
            // =====================================

            var approvedPaidLeaves =
                await _context.PaidLeaveRequests
                    .AsNoTracking()
                    .Where(p =>
                        employeeIds.Contains(
                            p.EmployeeId
                        ) &&
                        p.Status == "承認" &&
                        p.LeaveDate >= startDate &&
                        p.LeaveDate < endDate)
                    .ToListAsync();

            // =====================================
            // 社員別集計
            // =====================================

            var items =
                new List<
                    AdminMonthlySummaryItemViewModel>();

            foreach (var employee in employees)
            {
                var employeeStartDate =
                    employee.JoinDate.Date >
                    startDate
                        ? employee.JoinDate.Date
                        : startDate;

                var employeeWorkingDates =
                    companyWorkingDates
                        .Where(date =>
                            date >= employeeStartDate &&
                            date < endDate)
                        .OrderBy(date =>
                            date)
                        .ToList();

                var employeeHolidayDays =
                    companyHolidayDates.Count(date =>
                        date >= employeeStartDate &&
                        date < endDate
                    );

                var employeeAttendances =
                    attendances
                        .Where(a =>
                            a.EmployeeId ==
                            employee.EmployeeId)
                        .ToList();

                var attendanceDates =
                    employeeAttendances
                        .Where(a =>
                            a.ClockInTime.HasValue)
                        .Select(a =>
                            a.AttendanceDate.Date)
                        .Distinct()
                        .ToHashSet();

                // 管理者が欠勤として確定した所定労働日
                var absenceDates =
                    employeeAttendances
                        .Where(a =>
                            a.IsAbsent &&
                            companyWorkingDates.Contains(
                                a.AttendanceDate.Date
                            ))
                        .Select(a =>
                            a.AttendanceDate.Date)
                        .Distinct()
                        .ToHashSet();

                var employeePaidLeaves =
                    approvedPaidLeaves
                        .Where(p =>
                            p.EmployeeId ==
                            employee.EmployeeId &&
                            companyWorkingDates.Contains(
                                p.LeaveDate.Date
                            ))
                        .ToList();

                var paidLeaveDays =
                    employeePaidLeaves.Sum(p =>
                        p.Days
                    );

                double missingStampDays = 0;

                foreach (var workingDate
                         in employeeWorkingDates)
                {
                    // 当日と未来日は未打刻に含めない
                    if (workingDate >=
                        missingStampCheckEndDate)
                    {
                        continue;
                    }

                    // 出勤打刻がある場合
                    if (attendanceDates.Contains(
                            workingDate))
                    {
                        continue;
                    }

                    // 管理者が欠勤として確定した場合
                    if (absenceDates.Contains(
                            workingDate))
                    {
                        continue;
                    }

                    var approvedLeaveDays =
                        employeePaidLeaves
                            .Where(p =>
                                p.LeaveDate.Date ==
                                workingDate)
                            .Sum(p =>
                                p.Days);

                    // 1日有給なら未打刻0日、
                    // 半日有給なら未打刻0.5日
                    missingStampDays +=
                        Math.Max(
                            0,
                            1.0 -
                            approvedLeaveDays
                        );
                }

                items.Add(
                    new AdminMonthlySummaryItemViewModel
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        EmployeeName =
                            employee.Name,

                        DepartmentName =
                            employee.Department
                                ?.DepartmentName ?? "",

                        ScheduledWorkDays =
                            employeeWorkingDates.Count,

                        HolidayDays =
                            employeeHolidayDays,

                        WorkDays =
                            attendanceDates.Count,

                        PaidLeaveDays =
                            paidLeaveDays,

                        AbsenceDays =
                            absenceDates.Count,

                        MissingStampDays =
                            missingStampDays,

                        TotalWorkMinutes =
                            employeeAttendances.Sum(a =>
                                a.WorkMinutes),

                        TotalLateMinutes =
                            employeeAttendances.Sum(a =>
                                a.LateMinutes),

                        TotalOvertimeMinutes =
                            employeeAttendances.Sum(a =>
                                a.OvertimeMinutes)
                    }
                );
            }

            // =====================================
            // 画面表示用ViewModel
            // =====================================

            var viewModel =
                new AdminMonthlySummaryViewModel
                {
                    YearMonth =
                        startDate.ToString(
                            "yyyy-MM"
                        ),

                    DepartmentId =
                        departmentId,

                    Departments =
                        GetDepartments(),

                    IsCompanyCalendarGenerated =
                        isCompanyCalendarGenerated,

                    CompanyWorkingDayCount =
                        companyWorkingDates.Count,

                    CompanyHolidayCount =
                        companyHolidayDates.Count,

                    TotalEmployeeCount =
                        employees.Count,

                    TotalAttendanceDays =
                        items.Sum(i =>
                            i.WorkDays),

                    TotalPaidLeaveDays =
                        items.Sum(i =>
                            i.PaidLeaveDays),

                    TotalAbsenceDays =
                        items.Sum(i =>
                            i.AbsenceDays),

                    TotalMissingStampDays =
                        items.Sum(i =>
                            i.MissingStampDays),

                    TotalWorkMinutes =
                        items.Sum(i =>
                            i.TotalWorkMinutes),

                    TotalLateMinutes =
                        items.Sum(i =>
                            i.TotalLateMinutes),

                    TotalOvertimeMinutes =
                        items.Sum(i =>
                            i.TotalOvertimeMinutes),

                    Items =
                        items
                };

            return View(viewModel);
        }


        [HttpGet]
        public IActionResult MonthlyClosing(string? yearMonth)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            if (!TryParseYearMonth(yearMonth, out var targetMonth))
            {
                targetMonth = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1
                );

                if (!string.IsNullOrWhiteSpace(yearMonth))
                {
                    TempData["ErrorMessage"] =
                        "対象年月の形式が正しくありません。";
                }
            }

            var startDate =
                new DateTime(
                    targetMonth.Year,
                    targetMonth.Month,
                    1
                );

            var endDate =
                startDate.AddMonths(1);

            var closing = _context.MonthlyClosings
                .AsNoTracking()
                .Include(m => m.ClosedByEmployee)
                .Include(m => m.ReopenedByEmployee)
                .FirstOrDefault(m =>
                    m.TargetYear == startDate.Year &&
                    m.TargetMonth == startDate.Month);

            var attendanceCount =
                _context.Attendances
                    .AsNoTracking()
                    .Count(a =>
                        a.AttendanceDate >= startDate &&
                        a.AttendanceDate < endDate);

            var missingClockOutCount =
                _context.Attendances
                    .AsNoTracking()
                    .Count(a =>
                        a.AttendanceDate >= startDate &&
                        a.AttendanceDate < endDate &&
                        a.ClockInTime.HasValue &&
                        !a.ClockOutTime.HasValue);

            var pendingCorrectionRequestCount =
                _context.AttendanceCorrectionRequests
                    .AsNoTracking()
                    .Count(r =>
                        r.TargetDate >= startDate &&
                        r.TargetDate < endDate &&
                        r.Status == "申請中");
            var pendingPaidLeaveRequestCount =
    _context.PaidLeaveRequests
        .AsNoTracking()
        .Count(r =>
            r.LeaveDate >= startDate &&
            r.LeaveDate < endDate &&
            r.Status == "申請中");

            var history = _context.MonthlyClosings
                .AsNoTracking()
                .Include(m => m.ClosedByEmployee)
                .Include(m => m.ReopenedByEmployee)
                .OrderByDescending(m => m.TargetYear)
                .ThenByDescending(m => m.TargetMonth)
                .ToList()
                .Select(m =>
                    new MonthlyClosingHistoryItemViewModel
                    {
                        MonthlyClosingId =
                            m.MonthlyClosingId,

                        TargetYear =
                            m.TargetYear,

                        TargetMonth =
                            m.TargetMonth,

                        IsClosed =
                            m.IsClosed,

                        ClosedAt =
                            m.ClosedAt,

                        ClosedByEmployeeName =
                            m.ClosedByEmployee?.Name ?? "-",

                        ClosingComment =
                            m.ClosingComment ?? string.Empty,

                        ReopenedAt =
                            m.ReopenedAt,

                        ReopenedByEmployeeName =
                            m.ReopenedByEmployee?.Name ?? "-",

                        ReopenComment =
                            m.ReopenComment ?? string.Empty,

                        UpdatedAt =
                            m.UpdatedAt
                    })
                .ToList();

            var viewModel = new MonthlyClosingViewModel
            {
                YearMonth =
                    startDate.ToString("yyyy-MM"),

                TargetYear =
                    startDate.Year,

                TargetMonth =
                    startDate.Month,

                IsClosed =
                    closing?.IsClosed ?? false,

                AttendanceCount =
                    attendanceCount,

                MissingClockOutCount =
                    missingClockOutCount,

                PendingCorrectionRequestCount =
                    pendingCorrectionRequestCount,
                PendingPaidLeaveRequestCount =
                    pendingPaidLeaveRequestCount,

                ClosedAt =
                    closing?.ClosedAt,

                ClosedByEmployeeId =
                    closing?.ClosedByEmployeeId,

                ClosedByEmployeeName =
                    closing?.ClosedByEmployee?.Name ?? string.Empty,

                ClosingComment =
                    closing?.ClosingComment ?? string.Empty,

                ReopenedAt =
                    closing?.ReopenedAt,

                ReopenedByEmployeeId =
                    closing?.ReopenedByEmployeeId,

                ReopenedByEmployeeName =
                    closing?.ReopenedByEmployee?.Name ?? string.Empty,

                ReopenComment =
                    closing?.ReopenComment ?? string.Empty,

                History =
                    history
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseMonth(
         string? yearMonth,
         string? closingComment)
        {
            var role =
                HttpContext.Session.GetString(
                    "LoginUserRole"
                );

            var adminId =
                HttpContext.Session.GetInt32(
                    "LoginUserId"
                );

            if (role != "Admin" ||
                adminId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!TryParseYearMonth(
                    yearMonth,
                    out var targetMonth))
            {
                TempData["ErrorMessage"] =
                    "対象年月の形式が正しくありません。";

                return RedirectToAction(
                    "MonthlyClosing"
                );
            }

            var normalizedComment =
                string.IsNullOrWhiteSpace(
                    closingComment)
                    ? null
                    : closingComment.Trim();

            if (normalizedComment?.Length > 500)
            {
                TempData["ErrorMessage"] =
                    "締めコメントは500文字以内で入力してください。";

                return RedirectToAction(
                    "MonthlyClosing",
                    new
                    {
                        yearMonth =
                            targetMonth.ToString(
                                "yyyy-MM"
                            )
                    }
                );
            }

            var startDate =
                new DateTime(
                    targetMonth.Year,
                    targetMonth.Month,
                    1
                );

            var endDate =
                startDate.AddMonths(1);

            var today =
                DateTime.Today;

            // =====================================
            // すでに締め済みか確認
            // =====================================

            var closing =
                await _context.MonthlyClosings
                    .FirstOrDefaultAsync(m =>
                        m.TargetYear ==
                            startDate.Year &&
                        m.TargetMonth ==
                            startDate.Month
                    );

            if (closing?.IsClosed == true)
            {
                _operationLogService.Write(
                    actionName:
                        "月次締め",

                    targetType:
                        "MonthlyClosing",

                    targetId:
                        closing.MonthlyClosingId,

                    details:
                        $"締め済みの年月を再度締めようとしました。" +
                        $"対象年月：{startDate:yyyy年MM月}。",

                    result:
                        "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象年月はすでに締め済みです。";

                return RedirectToAction(
                    "MonthlyClosing",
                    new
                    {
                        yearMonth =
                            startDate.ToString(
                                "yyyy-MM"
                            )
                    }
                );
            }

            // =====================================
            // 会社カレンダー
            // =====================================

            var companyCalendarDays =
                await _companyCalendarService
                    .GetMonthAsync(
                        startDate.Year,
                        startDate.Month
                    );

            var expectedCalendarDayCount =
                DateTime.DaysInMonth(
                    startDate.Year,
                    startDate.Month
                );

            var isCompanyCalendarGenerated =
                companyCalendarDays.Count ==
                expectedCalendarDayCount;

            var companyWorkingDates =
                companyCalendarDays
                    .Where(c =>
                        c.IsWorkingDay)
                    .Select(c =>
                        c.CalendarDate.Date)
                    .Distinct()
                    .OrderBy(date =>
                        date)
                    .ToList();

            // 当日と未来日は未打刻判定に含めない
            var missingStampCheckEndDate =
                endDate <= today
                    ? endDate
                    : today;

            // =====================================
            // 既存の締め前チェック
            // =====================================

            var missingClockOutCount =
                await _context.Attendances
                    .AsNoTracking()
                    .CountAsync(a =>
                        a.AttendanceDate >=
                            startDate &&
                        a.AttendanceDate <
                            endDate &&
                        a.ClockInTime.HasValue &&
                        !a.ClockOutTime.HasValue
                    );

            var pendingCorrectionRequestCount =
                await _context
                    .AttendanceCorrectionRequests
                    .AsNoTracking()
                    .CountAsync(r =>
                        r.TargetDate >=
                            startDate &&
                        r.TargetDate <
                            endDate &&
                        r.Status ==
                            "申請中"
                    );

            var pendingPaidLeaveRequestCount =
                await _context.PaidLeaveRequests
                    .AsNoTracking()
                    .CountAsync(r =>
                        r.LeaveDate >=
                            startDate &&
                        r.LeaveDate <
                            endDate &&
                        r.Status ==
                            "申請中"
                    );

            // =====================================
            // 未打刻チェック
            // =====================================

            var employees =
                await _context.Employees
                    .AsNoTracking()
                    .Where(e =>
                        e.IsActive &&
                        e.Role == "Employee" &&
                        e.JoinDate <
                            endDate
                    )
                    .Select(e =>
                        new
                        {
                            e.EmployeeId,
                            e.JoinDate
                        }
                    )
                    .ToListAsync();

            var employeeIds =
                employees
                    .Select(e =>
                        e.EmployeeId)
                    .ToList();

            var attendances =
                await _context.Attendances
                    .AsNoTracking()
                    .Where(a =>
                        employeeIds.Contains(
                            a.EmployeeId
                        ) &&
                        a.AttendanceDate >=
                            startDate &&
                        a.AttendanceDate <
                            endDate
                    )
                    .Select(a =>
                        new
                        {
                            a.EmployeeId,
                            AttendanceDate =
                                a.AttendanceDate.Date,
                            a.ClockInTime,
                            a.IsAbsent
                        }
                    )
                    .ToListAsync();

            var approvedPaidLeaves =
                await _context.PaidLeaveRequests
                    .AsNoTracking()
                    .Where(p =>
                        employeeIds.Contains(
                            p.EmployeeId
                        ) &&
                        p.Status ==
                            "承認" &&
                        p.LeaveDate >=
                            startDate &&
                        p.LeaveDate <
                            endDate
                    )
                    .Select(p =>
                        new
                        {
                            p.EmployeeId,
                            LeaveDate =
                                p.LeaveDate.Date,
                            p.Days
                        }
                    )
                    .ToListAsync();

            double missingStampDays = 0;

            foreach (var employee
                     in employees)
            {
                var employeeStartDate =
                    employee.JoinDate.Date >
                    startDate
                        ? employee.JoinDate.Date
                        : startDate;

                var attendanceDates =
                    attendances
                        .Where(a =>
                            a.EmployeeId ==
                                employee.EmployeeId &&
                            a.ClockInTime.HasValue
                        )
                        .Select(a =>
                            a.AttendanceDate)
                        .Distinct()
                        .ToHashSet();

                var absenceDates =
                    attendances
                        .Where(a =>
                            a.EmployeeId ==
                                employee.EmployeeId &&
                            a.IsAbsent
                        )
                        .Select(a =>
                            a.AttendanceDate)
                        .Distinct()
                        .ToHashSet();

                var employeePaidLeaves =
                    approvedPaidLeaves
                        .Where(p =>
                            p.EmployeeId ==
                            employee.EmployeeId)
                        .ToList();

                foreach (var workingDate
                         in companyWorkingDates)
                {
                    if (workingDate <
                            employeeStartDate ||
                        workingDate >=
                            missingStampCheckEndDate)
                    {
                        continue;
                    }

                    // 出勤打刻済み
                    if (attendanceDates.Contains(
                            workingDate))
                    {
                        continue;
                    }

                    // 欠勤確定済み
                    if (absenceDates.Contains(
                            workingDate))
                    {
                        continue;
                    }

                    var approvedLeaveDays =
                        employeePaidLeaves
                            .Where(p =>
                                p.LeaveDate ==
                                workingDate)
                            .Sum(p =>
                                p.Days);

                    // 1日有給なら0日、
                    // 半日有給なら残り0.5日を未打刻とする
                    missingStampDays +=
                        Math.Max(
                            0,
                            1.0 -
                            approvedLeaveDays
                        );
                }
            }

            // =====================================
            // 月次締めブロック判定
            // =====================================

            if (!isCompanyCalendarGenerated ||
                missingClockOutCount > 0 ||
                pendingCorrectionRequestCount > 0 ||
                pendingPaidLeaveRequestCount > 0 ||
                missingStampDays > 0)
            {
                _operationLogService.Write(
                    actionName:
                        "月次締め",

                    targetType:
                        "MonthlyClosing",

                    details:
                        $"月次締めを中止しました。" +
                        $"対象年月：{startDate:yyyy年MM月}、" +
                        $"会社カレンダー未作成：" +
                        $"{(!isCompanyCalendarGenerated ? "あり" : "なし")}、" +
                        $"未打刻：{missingStampDays:0.#}日、" +
                        $"未退勤：{missingClockOutCount}件、" +
                        $"未処理の勤怠修正申請：" +
                        $"{pendingCorrectionRequestCount}件、" +
                        $"未処理の有給申請：" +
                        $"{pendingPaidLeaveRequestCount}件。",

                    result:
                        "失敗"
                );

                var errorMessages =
                    new List<string>();

                if (!isCompanyCalendarGenerated)
                {
                    errorMessages.Add(
                        "会社カレンダーが未作成です"
                    );
                }

                if (missingStampDays > 0)
                {
                    errorMessages.Add(
                        $"未打刻が{missingStampDays:0.#}日あります"
                    );
                }

                if (missingClockOutCount > 0)
                {
                    errorMessages.Add(
                        $"未退勤が{missingClockOutCount}件あります"
                    );
                }

                if (pendingCorrectionRequestCount > 0)
                {
                    errorMessages.Add(
                        "未処理の勤怠修正申請が" +
                        $"{pendingCorrectionRequestCount}件あります"
                    );
                }

                if (pendingPaidLeaveRequestCount > 0)
                {
                    errorMessages.Add(
                        "未処理の有給申請が" +
                        $"{pendingPaidLeaveRequestCount}件あります"
                    );
                }

                TempData["ErrorMessage"] =
                    string.Join(
                        "、",
                        errorMessages
                    ) +
                    "。すべて解決してから月次締めを実行してください。";

                return RedirectToAction(
                    "MonthlyClosing",
                    new
                    {
                        yearMonth =
                            startDate.ToString(
                                "yyyy-MM"
                            )
                    }
                );
            }

            // =====================================
            // 月次締め保存
            // =====================================

            var now =
                DateTime.Now;

            if (closing == null)
            {
                closing =
                    new MonthlyClosing
                    {
                        TargetYear =
                            startDate.Year,

                        TargetMonth =
                            startDate.Month,

                        IsClosed =
                            true,

                        ClosedAt =
                            now,

                        ClosedByEmployeeId =
                            adminId.Value,

                        ClosingComment =
                            normalizedComment,

                        CreatedAt =
                            now,

                        UpdatedAt =
                            now
                    };

                _context.MonthlyClosings.Add(
                    closing
                );
            }
            else
            {
                closing.IsClosed =
                    true;

                closing.ClosedAt =
                    now;

                closing.ClosedByEmployeeId =
                    adminId.Value;

                closing.ClosingComment =
                    normalizedComment;

                closing.UpdatedAt =
                    now;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _context.Entry(
                    closing
                ).State =
                    EntityState.Detached;

                _operationLogService.Write(
                    actionName:
                        "月次締め",

                    targetType:
                        "MonthlyClosing",

                    details:
                        $"月次締めの保存中に競合が発生しました。" +
                        $"対象年月：{startDate:yyyy年MM月}。",

                    result:
                        "失敗"
                );

                TempData["ErrorMessage"] =
                    "月次締めの保存中にエラーが発生しました。" +
                    "画面を更新してから再度実行してください。";

                return RedirectToAction(
                    "MonthlyClosing",
                    new
                    {
                        yearMonth =
                            startDate.ToString(
                                "yyyy-MM"
                            )
                    }
                );
            }

            var commentText =
                string.IsNullOrWhiteSpace(
                    normalizedComment)
                    ? "なし"
                    : normalizedComment;

            _operationLogService.Write(
                actionName:
                    "月次締め",

                targetType:
                    "MonthlyClosing",

                targetId:
                    closing.MonthlyClosingId,

                details:
                    $"月次締めを実行しました。" +
                    $"対象年月：{startDate:yyyy年MM月}、" +
                    $"締めコメント：{commentText}。",

                result:
                    "成功"
            );

            TempData["SuccessMessage"] =
                $"{startDate:yyyy年MM月}の月次締めを実行しました。";

            return RedirectToAction(
                "MonthlyClosing",
                new
                {
                    yearMonth =
                        startDate.ToString(
                            "yyyy-MM"
                        )
                }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReopenMonth(
            string? yearMonth,
            string? reopenComment)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var adminId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Admin" || adminId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!TryParseYearMonth(yearMonth, out var targetMonth))
            {
                TempData["ErrorMessage"] =
                    "対象年月の形式が正しくありません。";

                return RedirectToAction("MonthlyClosing");
            }

            if (string.IsNullOrWhiteSpace(reopenComment))
            {
                TempData["ErrorMessage"] =
                    "再開する場合は、再開理由を入力してください。";

                return RedirectToAction(
                    "MonthlyClosing",
                    new
                    {
                        yearMonth =
                            targetMonth.ToString("yyyy-MM")
                    });
            }

            var normalizedComment =
                reopenComment.Trim();

            if (normalizedComment.Length > 500)
            {
                TempData["ErrorMessage"] =
                    "再開理由は500文字以内で入力してください。";

                return RedirectToAction(
                    "MonthlyClosing",
                    new
                    {
                        yearMonth =
                            targetMonth.ToString("yyyy-MM")
                    });
            }

            var closing =
                _context.MonthlyClosings
                    .FirstOrDefault(m =>
                        m.TargetYear == targetMonth.Year &&
                        m.TargetMonth == targetMonth.Month);

            if (closing == null || !closing.IsClosed)
            {
                _operationLogService.Write(
                    actionName: "月次締め解除",
                    targetType: "MonthlyClosing",
                    targetId:
                        closing?.MonthlyClosingId,
                    details:
                        $"締め済みではない年月を再開しようとしました。" +
                        $"対象年月：{targetMonth:yyyy年MM月}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象年月は締め済みではありません。";

                return RedirectToAction(
                    "MonthlyClosing",
                    new
                    {
                        yearMonth =
                            targetMonth.ToString("yyyy-MM")
                    });
            }

            closing.IsClosed = false;
            closing.ReopenedAt = DateTime.Now;
            closing.ReopenedByEmployeeId = adminId.Value;
            closing.ReopenComment = normalizedComment;
            closing.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            _operationLogService.Write(
                actionName: "月次締め解除",
                targetType: "MonthlyClosing",
                targetId: closing.MonthlyClosingId,
                details:
                    $"月次締めを解除しました。" +
                    $"対象年月：{targetMonth:yyyy年MM月}、" +
                    $"再開理由：{normalizedComment}。",
                result: "成功"
            );

            TempData["SuccessMessage"] =
                $"{targetMonth:yyyy年MM月}の月次締めを解除しました。";

            return RedirectToAction(
                "MonthlyClosing",
                new
                {
                    yearMonth =
                        targetMonth.ToString("yyyy-MM")
                });
        }

        public IActionResult CorrectionRequests()
        {
            var role = HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var requests = _context.AttendanceCorrectionRequests
                .Include(r => r.Employee)
                .OrderBy(r => r.Status == "申請中" ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();

            var viewModel = new AdminCorrectionRequestViewModel
            {
                Requests = requests
                    .Select(r =>
                        new AdminCorrectionRequestItemViewModel
                        {
                            RequestId = r.RequestId,

                            EmployeeName =
                                r.Employee != null
                                    ? r.Employee.Name
                                    : "",

                            TargetDate = r.TargetDate,
                            CorrectionType = r.CorrectionType,
                            BeforeTime = r.BeforeTime,
                            AfterTime = r.AfterTime,
                            Reason = r.Reason,
                            Status = r.Status,
                            CreatedAt = r.CreatedAt
                        })
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveCorrection(
     int id,
     string? adminComment)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var adminId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Admin" || adminId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var request = _context.AttendanceCorrectionRequests
                .Include(r => r.Attendance)
                .Include(r => r.Employee)
                .FirstOrDefault(r => r.RequestId == id);

            if (request == null ||
                request.Attendance == null)
            {
                _operationLogService.Write(
                    actionName: "勤怠修正承認",
                    targetType: "AttendanceCorrectionRequest",
                    targetId: id,
                    details:
                        $"対象の勤怠修正申請が見つかりません。" +
                        $"申請ID：{id}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象の勤怠修正申請が見つかりません。";

                return RedirectToAction("CorrectionRequests");
            }

            if (request.Status != "申請中")
            {
                _operationLogService.Write(
                    actionName: "勤怠修正承認",
                    targetType: "AttendanceCorrectionRequest",
                    targetId: request.RequestId,
                    details:
                        $"処理済みの申請を承認しようとしました。" +
                        $"現在の状態：{request.Status}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "この申請はすでに処理されています。";

                return RedirectToAction("CorrectionRequests");
            }

            if (_monthlyClosingService.IsClosed(request.TargetDate))
            {
                _operationLogService.Write(
                    actionName: "勤怠修正承認",
                    targetType: "AttendanceCorrectionRequest",
                    targetId: request.RequestId,
                    details:
                        $"月次締め済みのため、勤怠修正申請の承認を拒否しました。" +
                        $"対象日：{request.TargetDate:yyyy/MM/dd}、" +
                        $"申請ID：{request.RequestId}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    $"{request.TargetDate:yyyy年MM月}は月次締め済みのため、" +
                    "勤怠修正申請を承認できません。";

                return RedirectToAction("CorrectionRequests");
            }

            if (request.CorrectionType != "出勤時間" &&
                request.CorrectionType != "退勤時間")
            {
                TempData["ErrorMessage"] =
                    "修正項目が正しくありません。";

                return RedirectToAction("CorrectionRequests");
            }

            var attendance =
     request.Attendance;

            var employee =
                request.Employee;

            if (employee == null)
            {
                _operationLogService.Write(
                    actionName: "勤怠修正承認",
                    targetType: "AttendanceCorrectionRequest",
                    targetId: request.RequestId,
                    details:
                        $"対象社員の情報が見つかりません。" +
                        $"社員ID：{request.EmployeeId}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象社員の情報が見つかりません。";

                return RedirectToAction(
                    "CorrectionRequests"
                );
            }

            var beforeTimeText =
                request.BeforeTime.HasValue
                    ? request.BeforeTime.Value
                        .ToString(@"hh\:mm")
                    : "-";

            var afterTimeText =
                request.AfterTime
                    .ToString(@"hh\:mm");

            // 修正後の出勤・退勤時刻を一時的に作成
            var correctedClockInTime =
                attendance.ClockInTime;

            var correctedClockOutTime =
                attendance.ClockOutTime;

            if (request.CorrectionType == "出勤時間")
            {
                correctedClockInTime =
                    request.AfterTime;
            }
            else
            {
                correctedClockOutTime =
                    request.AfterTime;
            }

            // 退勤時刻が出勤時刻以前の場合は承認しない
            if (correctedClockInTime.HasValue &&
                correctedClockOutTime.HasValue &&
                correctedClockOutTime.Value <=
                correctedClockInTime.Value)
            {
                _operationLogService.Write(
                    actionName: "勤怠修正承認",
                    targetType: "AttendanceCorrectionRequest",
                    targetId: request.RequestId,
                    details:
                        $"修正後の退勤時刻が出勤時刻以前のため、" +
                        $"承認を中止しました。" +
                        $"出勤時刻：" +
                        $"{correctedClockInTime.Value:hh\\:mm}、" +
                        $"退勤時刻：" +
                        $"{correctedClockOutTime.Value:hh\\:mm}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "退勤時刻は出勤時刻より後に設定してください。";

                return RedirectToAction(
                    "CorrectionRequests"
                );
            }

            // 修正時刻を勤怠データへ反映
            attendance.ClockInTime =
                correctedClockInTime;

            attendance.ClockOutTime =
                correctedClockOutTime;

            // 出勤・退勤が両方ある場合
            if (attendance.ClockInTime.HasValue &&
                attendance.ClockOutTime.HasValue)
            {
                var calculation =
                 _attendanceCalculationService.Calculate(
                       attendance,
                       attendance.ClockInTime.Value,
                       attendance.ClockOutTime.Value
                          );

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
            }
            // 出勤時刻だけある場合
            else if (attendance.ClockInTime.HasValue)
            {
                attendance.BreakMinutes = 0;
                attendance.WorkMinutes = 0;

                attendance.LateMinutes =
                    _attendanceCalculationService
                     .CalculateLateMinutes(
                      attendance,
                       attendance.ClockInTime.Value
                      );

                attendance.OvertimeMinutes = 0;

                attendance.Status =
                    attendance.LateMinutes > 0
                        ? "遅刻"
                        : "出勤中";
            }
            // 出勤時刻もない場合
            else
            {
                attendance.BreakMinutes = 0;
                attendance.WorkMinutes = 0;
                attendance.LateMinutes = 0;
                attendance.OvertimeMinutes = 0;
                attendance.Status = "未出勤";
            }

            attendance.UpdatedAt =
                DateTime.Now;

            var normalizedComment =
                string.IsNullOrWhiteSpace(adminComment)
                    ? null
                    : adminComment.Trim();

            if (normalizedComment?.Length > 500)
            {
                TempData["ErrorMessage"] =
                    "管理者コメントは500文字以内で入力してください。";

                return RedirectToAction("CorrectionRequests");
            }

            request.Status = "承認";
            request.ApprovedAt = DateTime.Now;
            request.ApprovedBy = adminId.Value;
            request.AdminComment = normalizedComment;

            _context.SaveChanges();

            var employeeName =
                request.Employee?.Name ?? "不明";

            var commentText =
                string.IsNullOrWhiteSpace(normalizedComment)
                    ? "なし"
                    : normalizedComment;

            _operationLogService.Write(
                actionName: "勤怠修正承認",
                targetType: "AttendanceCorrectionRequest",
                targetId: request.RequestId,
                details:
                    $"勤怠修正申請を承認しました。" +
                    $"社員：{employeeName}、" +
                    $"対象日：{request.TargetDate:yyyy/MM/dd}、" +
                    $"修正項目：{request.CorrectionType}、" +
                    $"修正前：{beforeTimeText}、" +
                    $"修正後：{afterTimeText}、" +
                    $"管理者コメント：{commentText}。",
                result: "成功"
            );

            TempData["SuccessMessage"] =
                "勤怠修正申請を承認しました。";

            return RedirectToAction("CorrectionRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectCorrection(
    int id,
    string? adminComment)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var adminId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Admin" || adminId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var request = _context.AttendanceCorrectionRequests
                .Include(r => r.Employee)
                .FirstOrDefault(r => r.RequestId == id);

            if (request == null)
            {
                _operationLogService.Write(
                    actionName: "勤怠修正却下",
                    targetType: "AttendanceCorrectionRequest",
                    targetId: id,
                    details:
                        $"対象の勤怠修正申請が見つかりません。" +
                        $"申請ID：{id}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象の勤怠修正申請が見つかりません。";

                return RedirectToAction("CorrectionRequests");
            }

            if (request.Status != "申請中")
            {
                _operationLogService.Write(
                    actionName: "勤怠修正却下",
                    targetType: "AttendanceCorrectionRequest",
                    targetId: request.RequestId,
                    details:
                        $"処理済みの申請を却下しようとしました。" +
                        $"現在の状態：{request.Status}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "この申請はすでに処理されています。";

                return RedirectToAction("CorrectionRequests");
            }

            if (string.IsNullOrWhiteSpace(adminComment))
            {
                _operationLogService.Write(
                    actionName: "勤怠修正却下",
                    targetType: "AttendanceCorrectionRequest",
                    targetId: request.RequestId,
                    details:
                        "却下理由が入力されていないため、処理を中止しました。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "却下する場合は、管理者コメントに却下理由を入力してください。";

                return RedirectToAction("CorrectionRequests");
            }

            var normalizedComment = adminComment.Trim();

            if (normalizedComment.Length > 500)
            {
                TempData["ErrorMessage"] =
                    "管理者コメントは500文字以内で入力してください。";

                return RedirectToAction("CorrectionRequests");
            }

            request.Status = "却下";
            request.ApprovedAt = DateTime.Now;
            request.ApprovedBy = adminId.Value;
            request.AdminComment = normalizedComment;

            _context.SaveChanges();

            var employeeName =
                request.Employee?.Name ?? "不明";

            var beforeTimeText =
                request.BeforeTime.HasValue
                    ? request.BeforeTime.Value.ToString(@"hh\:mm")
                    : "-";

            var afterTimeText =
                request.AfterTime.ToString(@"hh\:mm");

            _operationLogService.Write(
                actionName: "勤怠修正却下",
                targetType: "AttendanceCorrectionRequest",
                targetId: request.RequestId,
                details:
                    $"勤怠修正申請を却下しました。" +
                    $"社員：{employeeName}、" +
                    $"対象日：{request.TargetDate:yyyy/MM/dd}、" +
                    $"修正項目：{request.CorrectionType}、" +
                    $"修正前：{beforeTimeText}、" +
                    $"修正希望：{afterTimeText}、" +
                    $"却下理由：{normalizedComment}。",
                result: "成功"
            );

            TempData["SuccessMessage"] =
                "勤怠修正申請を却下しました。";

            return RedirectToAction("CorrectionRequests");
        }


        public IActionResult PaidLeaveRequests()
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var requests = _context.PaidLeaveRequests
                .AsNoTracking()
                .Include(r => r.Employee)
                .OrderBy(r => r.Status == "申請中" ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();

            var viewModel = new AdminPaidLeaveRequestViewModel
            {
                Requests = requests
                    .Select(r =>
                        new AdminPaidLeaveRequestItemViewModel
                        {
                            PaidLeaveRequestId =
                                r.PaidLeaveRequestId,

                            EmployeeName =
                                r.Employee != null
                                    ? r.Employee.Name
                                    : "",

                            LeaveDate = r.LeaveDate,
                            Days = r.Days,
                            Reason = r.Reason,
                            Status = r.Status,
                            CreatedAt = r.CreatedAt
                        })
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApprovePaidLeave(
     int id,
     string? adminComment)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var adminId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Admin" || adminId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var normalizedAdminComment =
                string.IsNullOrWhiteSpace(adminComment)
                    ? null
                    : adminComment.Trim();

            if (normalizedAdminComment?.Length > 500)
            {
                _operationLogService.Write(
                    actionName: "有給申請承認",
                    targetType: "PaidLeaveRequest",
                    targetId: id,
                    details:
                        "管理者コメントが500文字を超えているため、" +
                        "承認処理を中止しました。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "管理者コメントは500文字以内で入力してください。";

                return RedirectToAction("PaidLeaveRequests");
            }

            var request = _context.PaidLeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefault(r =>
                    r.PaidLeaveRequestId == id);

            if (request == null)
            {
                _operationLogService.Write(
                    actionName: "有給申請承認",
                    targetType: "PaidLeaveRequest",
                    targetId: id,
                    details:
                        $"対象の有給申請が見つかりません。" +
                        $"申請ID：{id}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象の有給申請が見つかりません。";

                return RedirectToAction("PaidLeaveRequests");
            }

            if (request.Status != "申請中")
            {
                _operationLogService.Write(
                    actionName: "有給申請承認",
                    targetType: "PaidLeaveRequest",
                    targetId: request.PaidLeaveRequestId,
                    details:
                        $"処理済みの有給申請を承認しようとしました。" +
                        $"現在の状態：{request.Status}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "この有給申請はすでに処理されています。";

                return RedirectToAction("PaidLeaveRequests");
            }

            if (_monthlyClosingService.IsClosed(request.LeaveDate))
            {
                _operationLogService.Write(
                    actionName: "有給申請承認",
                    targetType: "PaidLeaveRequest",
                    targetId: request.PaidLeaveRequestId,
                    details:
                        $"月次締め済みのため、有給申請の承認を拒否しました。" +
                        $"取得日：{request.LeaveDate:yyyy/MM/dd}、" +
                        $"申請ID：{request.PaidLeaveRequestId}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    $"{request.LeaveDate:yyyy年MM月}は月次締め済みのため、" +
                    "有給申請を承認できません。";

                return RedirectToAction("PaidLeaveRequests");
            }

            if (request.Employee == null)
            {
                _operationLogService.Write(
                    actionName: "有給申請承認",
                    targetType: "PaidLeaveRequest",
                    targetId: request.PaidLeaveRequestId,
                    details:
                        $"申請者の社員情報が見つかりません。" +
                        $"社員ID：{request.EmployeeId}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "申請者の社員情報が見つかりません。";

                return RedirectToAction("PaidLeaveRequests");
            }

            var employee = request.Employee;

            var legalResult =
    CalculatePaidLeaveRule(employee);

            if (!legalResult.IsEligible ||
                !legalResult.IsAttendanceRateEnough)
            {
                _operationLogService.Write(
                    actionName: "有給申請承認",
                    targetType: "PaidLeaveRequest",
                    targetId: request.PaidLeaveRequestId,
                    details:
                        $"有給付与条件を満たしていないため承認できません。" +
                        $"社員：{employee.Name}、" +
                        $"出勤率：{legalResult.AttendanceRate:0.#}％。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "有給付与条件を満たしていないため、承認できません。";

                return RedirectToAction(
                    "PaidLeaveRequests"
                );
            }

            if (!legalResult.CurrentGrantDate.HasValue)
            {
                _operationLogService.Write(
                    actionName: "有給申請承認",
                    targetType: "PaidLeaveRequest",
                    targetId: request.PaidLeaveRequestId,
                    details:
                        $"有給付与日が未到来のため承認できません。" +
                        $"社員：{employee.Name}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "有給付与日が未到来のため、承認できません。";

                return RedirectToAction(
                    "PaidLeaveRequests"
                );
            }

            if (request.LeaveDate.Date <
                legalResult.CurrentGrantDate.Value.Date)
            {
                _operationLogService.Write(
                    actionName: "有給申請承認",
                    targetType: "PaidLeaveRequest",
                    targetId: request.PaidLeaveRequestId,
                    details:
                        $"有給付与日前の日付のため承認できません。" +
                        $"取得希望日：{request.LeaveDate:yyyy/MM/dd}、" +
                        $"有給付与日：" +
                        $"{legalResult.CurrentGrantDate.Value:yyyy/MM/dd}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "有給付与日前の日付は承認できません。";

                return RedirectToAction(
                    "PaidLeaveRequests"
                );
            }

            // 未来日の申請の場合は、取得希望日時点の
            // 有給残高を基準に確認する
            var balanceCalculationTargetDate =
                request.LeaveDate.Date >
                DateTime.Today
                    ? request.LeaveDate.Date
                    : DateTime.Today;

            using var transaction =
                _context.Database.BeginTransaction();

            // 現在の付与判定結果を
            // 有給付与履歴へ登録または更新する
            _paidLeaveGrantHistoryService
                .SynchronizeCurrentGrantHistory(
                    employee,
                    legalResult
                );

            // 承認前の繰越・失効を含む残日数を計算する
            var calculationBeforeApproval =
                _paidLeaveBalanceCalculationService
                    .Calculate(
                        employee.EmployeeId,
                        balanceCalculationTargetDate
                    );

            var remainingDaysBeforeApproval =
                calculationBeforeApproval.RemainingDays;

            if (request.Days >
                remainingDaysBeforeApproval)
            {
                transaction.Rollback();

                _operationLogService.Write(
                    actionName: "有給申請承認",
                    targetType: "PaidLeaveRequest",
                    targetId: request.PaidLeaveRequestId,
                    details:
                        $"有給残日数不足のため承認できません。" +
                        $"社員：{employee.Name}、" +
                        $"残日数：{remainingDaysBeforeApproval:0.#}日、" +
                        $"申請日数：{request.Days:0.#}日。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "有給残日数が不足しているため、承認できません。";

                return RedirectToAction(
                    "PaidLeaveRequests"
                );
            }

            // 申請を承認する
            request.Status =
                "承認";

            request.ApprovedAt =
                DateTime.Now;

            request.ApprovedBy =
                adminId.Value;

            request.AdminComment =
                normalizedAdminComment;

            // 承認済み申請として一度保存する
            _context.SaveChanges();

            // 承認後の残高を再計算する
            var calculationAfterApproval =
                _paidLeaveBalanceCalculationService
                    .Calculate(
                        employee.EmployeeId,
                        balanceCalculationTargetDate
                    );

            var balanceYear =
                DateTime.Today.Year;

            var balance =
                _context.PaidLeaveBalances
                    .FirstOrDefault(b =>
                        b.EmployeeId ==
                            employee.EmployeeId &&
                        b.Year ==
                            balanceYear
                    );

            var now =
                DateTime.Now;

            if (balance == null)
            {
                balance =
                    new PaidLeaveBalance
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        Year =
                            balanceYear,

                        CreatedAt =
                            now
                    };

                _context.PaidLeaveBalances
                    .Add(balance);
            }

            balance.CurrentGrantedDays =
                calculationAfterApproval.CurrentGrantedDays;

            balance.CarriedOverDays =
                calculationAfterApproval.CarriedOverDays;

            balance.ExpiredDays =
                calculationAfterApproval.ExpiredDays;

            balance.GrantedDays =
                calculationAfterApproval.GrantedDays;

            balance.UsedDays =
                calculationAfterApproval.UsedDays;
            balance.ReservedDays =
            calculationAfterApproval.ReservedDays;

            balance.RemainingDays =
                calculationAfterApproval.RemainingDays;

            balance.CurrentGrantDate =
                calculationAfterApproval.CurrentGrantDate;

            balance.CurrentGrantExpiryDate =
                calculationAfterApproval.CurrentGrantExpiryDate;

            balance.LastCalculatedAt =
                now;

            balance.UpdatedAt =
                now;

            _context.SaveChanges();

            transaction.Commit();

            var commentText =
                string.IsNullOrWhiteSpace(normalizedAdminComment)
                    ? "なし"
                    : normalizedAdminComment;

            _operationLogService.Write(
                actionName: "有給申請承認",
                targetType: "PaidLeaveRequest",
                targetId: request.PaidLeaveRequestId,
                details:
                    $"有給申請を承認しました。" +
                    $"社員：{employee.Name}、" +
                    $"取得日：{request.LeaveDate:yyyy/MM/dd}、" +
                    $"申請日数：{request.Days:0.#}日、" +
                    $"承認前残日数：{remainingDaysBeforeApproval:0.#}日、" +
                    $"承認後残日数：{balance.RemainingDays:0.#}日、" +
                    $"管理者コメント：{commentText}。",
                result: "成功"
            );

            TempData["SuccessMessage"] =
                "有給申請を承認しました。";

            return RedirectToAction("PaidLeaveRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectPaidLeave(
      int id,
      string? adminComment)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var adminId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Admin" || adminId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var request = _context.PaidLeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefault(r =>
                    r.PaidLeaveRequestId == id);

            if (request == null)
            {
                _operationLogService.Write(
                    actionName: "有給申請却下",
                    targetType: "PaidLeaveRequest",
                    targetId: id,
                    details:
                        $"対象の有給申請が見つかりません。" +
                        $"申請ID：{id}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象の有給申請が見つかりません。";

                return RedirectToAction("PaidLeaveRequests");
            }

            if (request.Status != "申請中")
            {
                _operationLogService.Write(
                    actionName: "有給申請却下",
                    targetType: "PaidLeaveRequest",
                    targetId: request.PaidLeaveRequestId,
                    details:
                        $"処理済みの有給申請を却下しようとしました。" +
                        $"現在の状態：{request.Status}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "この有給申請はすでに処理されています。";

                return RedirectToAction("PaidLeaveRequests");
            }

            if (string.IsNullOrWhiteSpace(adminComment))
            {
                _operationLogService.Write(
                    actionName: "有給申請却下",
                    targetType: "PaidLeaveRequest",
                    targetId: request.PaidLeaveRequestId,
                    details:
                        "却下理由が入力されていないため、" +
                        "却下処理を中止しました。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "却下する場合は、管理者コメントに却下理由を入力してください。";

                return RedirectToAction("PaidLeaveRequests");
            }

            var normalizedAdminComment =
                adminComment.Trim();

            if (normalizedAdminComment.Length > 500)
            {
                _operationLogService.Write(
                    actionName: "有給申請却下",
                    targetType: "PaidLeaveRequest",
                    targetId: request.PaidLeaveRequestId,
                    details:
                        "管理者コメントが500文字を超えているため、" +
                        "却下処理を中止しました。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "管理者コメントは500文字以内で入力してください。";

                return RedirectToAction("PaidLeaveRequests");
            }

            request.Status = "却下";
            request.ApprovedAt = DateTime.Now;
            request.ApprovedBy = adminId.Value;
            request.AdminComment = normalizedAdminComment;

            _context.SaveChanges();

            var employeeName =
                request.Employee?.Name ?? "不明";

            _operationLogService.Write(
                actionName: "有給申請却下",
                targetType: "PaidLeaveRequest",
                targetId: request.PaidLeaveRequestId,
                details:
                    $"有給申請を却下しました。" +
                    $"社員：{employeeName}、" +
                    $"社員ID：{request.EmployeeId}、" +
                    $"取得日：{request.LeaveDate:yyyy/MM/dd}、" +
                    $"申請日数：{request.Days:0.#}日、" +
                    $"却下理由：{normalizedAdminComment}。",
                result: "成功"
            );

            TempData["SuccessMessage"] =
                "有給申請を却下しました。";

            return RedirectToAction("PaidLeaveRequests");
        }


        public IActionResult PaidLeaveAlerts()
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new AdminPaidLeaveAlertViewModel
            {
                Alerts = GetPaidLeaveAlertItems()
            };

            return View(viewModel);
        }

        public IActionResult PaidLeaveNotice(int employeeId)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .FirstOrDefault(e =>
                    e.EmployeeId == employeeId &&
                    e.IsActive);

            if (employee == null)
            {
                TempData["ErrorMessage"] =
                    "対象の社員が見つかりません。";

                return RedirectToAction("PaidLeaveAlerts");
            }

            var result = CalculatePaidLeaveRule(employee);

            if (!result.IsFiveDayAlertTarget)
            {
                TempData["ErrorMessage"] =
                    "この社員は現在、有給取得アラートの対象ではありません。";

                return RedirectToAction("PaidLeaveAlerts");
            }

            var grantDateText =
                result.CurrentGrantDate.HasValue
                    ? result.CurrentGrantDate.Value
                        .ToString("yyyy/MM/dd")
                    : "-";

            var deadlineText =
                result.FiveDayDeadline.HasValue
                    ? result.FiveDayDeadline.Value
                        .ToString("yyyy/MM/dd")
                    : "-";

            var noticeText = string.Join(
                Environment.NewLine,
                new[]
                {
                    $"{employee.Name} さん",
                    "",
                    "お疲れ様です。",
                    "有給休暇の取得状況についてご連絡いたします。",
                    "",
                    $"現在、年5日取得義務に対して、あと " +
                    $"{result.RemainingFiveDayRequirement:0.#} " +
                    $"日の取得が必要です。",
                    $"有給付与日：{grantDateText}",
                    $"取得期限：{deadlineText}",
                    $"現在の取得日数：" +
                    $"{result.UsedDaysAfterCurrentGrant:0.#} 日",
                    "",
                    "期限までに計画的に有給休暇を" +
                    "取得していただきますようお願いいたします。",
                    "",
                    "よろしくお願いいたします。"
                });

            var viewModel =
                new AdminPaidLeaveNoticeViewModel
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = employee.Name,

                    DepartmentName =
                        employee.Department?.DepartmentName ?? "",

                    CurrentGrantDate = result.CurrentGrantDate,
                    FiveDayDeadline = result.FiveDayDeadline,

                    UsedDays =
                        result.UsedDaysAfterCurrentGrant,

                    RemainingFiveDayRequirement =
                        result.RemainingFiveDayRequirement,

                    NoticeText = noticeText
                };

            return View(viewModel);
        }

        private List<AdminPaidLeaveAlertItemViewModel>
            GetPaidLeaveAlertItems()
        {
            var employees = _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Where(e => e.IsActive)
                .OrderBy(e => e.EmployeeId)
                .ToList();

            var alerts =
                new List<AdminPaidLeaveAlertItemViewModel>();

            foreach (var employee in employees)
            {
                var result =
                    CalculatePaidLeaveRule(employee);

                if (!result.IsFiveDayAlertTarget)
                {
                    continue;
                }

                var remainingDays = Math.Max(
                    0,
                    result.GrantedDays -
                    result.UsedDaysAfterCurrentGrant
                );

                alerts.Add(
                    new AdminPaidLeaveAlertItemViewModel
                    {
                        EmployeeId = employee.EmployeeId,
                        EmployeeName = employee.Name,

                        DepartmentName =
                            employee.Department?.DepartmentName ?? "",

                        JoinDate = employee.JoinDate,
                        CurrentGrantDate = result.CurrentGrantDate,
                        FiveDayDeadline = result.FiveDayDeadline,
                        LegalExpiryDate = result.LegalExpiryDate,
                        GrantedDays = result.GrantedDays,

                        UsedDays =
                            result.UsedDaysAfterCurrentGrant,

                        RemainingDays = remainingDays,

                        RemainingFiveDayRequirement =
                            result.RemainingFiveDayRequirement,

                        AttendanceRate = result.AttendanceRate,

                        IsAttendanceRateEnough =
                            result.IsAttendanceRateEnough,

                        IsFiveDayAlertTarget =
                            result.IsFiveDayAlertTarget,

                        StatusText = "要対応"
                    });
            }

            return alerts;
        }

        private PaidLeaveRuleResult CalculatePaidLeaveRule(
            Employee employee)
        {
            var today = DateTime.Today;

            var firstResult =
                _paidLeaveRuleService.Calculate(
                    employee.JoinDate,
                    today,
                    1,
                    1,
                    0
                );

            if (firstResult.CurrentGrantDate == null)
            {
                return _paidLeaveRuleService.Calculate(
                    employee.JoinDate,
                    today,
                    0,
                    0,
                    0
                );
            }

            var checkStartDate =
                firstResult.AttendanceCheckStartDate;

            var checkEndDate =
                firstResult.AttendanceCheckEndDate;

            var checkEndDateExclusive =
                checkEndDate.AddDays(1);

            // 会社カレンダーを基準に
            // 出勤率計算対象の所定労働日を取得する
            var companyWorkingDates =
                GetCompanyWorkingDates(
                    checkStartDate,
                    checkEndDate
                );

            var totalWorkDays =
                companyWorkingDates.Count;

            // 実際に出勤打刻した日
            var actualAttendanceDates =
                _context.Attendances
                    .AsNoTracking()
                    .Where(a =>
                        a.EmployeeId ==
                            employee.EmployeeId &&
                        a.AttendanceDate >=
                            checkStartDate &&
                        a.AttendanceDate <
                            checkEndDateExclusive &&
                        a.ClockInTime.HasValue)
                    .Select(a =>
                        a.AttendanceDate)
                    .ToList()
                    .Select(date =>
                        date.Date)
                    .Where(date =>
                        companyWorkingDates.Contains(
                            date
                        ))
                    .ToHashSet();

            // 承認済み有給取得日
            var approvedPaidLeaveDates =
                _context.PaidLeaveRequests
                    .AsNoTracking()
                    .Where(p =>
                        p.EmployeeId ==
                            employee.EmployeeId &&
                        p.Status == "承認" &&
                        p.Days > 0 &&
                        p.LeaveDate >=
                            checkStartDate &&
                        p.LeaveDate <
                            checkEndDateExclusive)
                    .Select(p =>
                        p.LeaveDate)
                    .ToList()
                    .Select(date =>
                        date.Date)
                    .Where(date =>
                        companyWorkingDates.Contains(
                            date
                        ))
                    .ToHashSet();

            // 実出勤日と有給取得日を統合する。
            // 同じ日は二重に数えない。
            actualAttendanceDates.UnionWith(
                approvedPaidLeaveDates
            );

            var attendedDays =
                actualAttendanceDates.Count;

            var currentGrantDate =
                firstResult.CurrentGrantDate.Value;

            var usedDaysAfterCurrentGrant =
                _context.PaidLeaveRequests
                    .AsNoTracking()
                    .Where(p =>
                        p.EmployeeId == employee.EmployeeId &&
                        p.Status == "承認" &&
                        p.LeaveDate >= currentGrantDate &&
                        p.LeaveDate <= today)
                    .Sum(p => p.Days);

            return _paidLeaveRuleService.Calculate(
                employee.JoinDate,
                today,
                totalWorkDays,
                attendedDays,
                usedDaysAfterCurrentGrant
            );
        }

        private HashSet<DateTime>
     GetCompanyWorkingDates(
         DateTime startDate,
         DateTime endDate)
        {
            var workingDates =
                new HashSet<DateTime>();

            startDate =
                startDate.Date;

            endDate =
                endDate.Date;

            if (endDate < startDate)
            {
                return workingDates;
            }

            var calendarRows =
                _context.CompanyCalendarDays
                    .AsNoTracking()
                    .Where(c =>
                        c.CalendarDate >=
                            startDate &&
                        c.CalendarDate <=
                            endDate)
                    .ToList()
                    .ToDictionary(
                        c => c.CalendarDate.Date,
                        c => c.IsWorkingDay
                    );

            var currentDate =
                startDate;

            while (currentDate <= endDate)
            {
                bool isWorkingDay;

                if (calendarRows.TryGetValue(
                        currentDate,
                        out var calendarWorkingDay))
                {
                    // 作成済みの日付は
                    // 会社カレンダーを使用する
                    isWorkingDay =
                        calendarWorkingDay;
                }
                else
                {
                    // 未作成期間は一時的に
                    // 月曜日～金曜日を勤務日とする
                    isWorkingDay =
                        currentDate.DayOfWeek !=
                            DayOfWeek.Saturday &&
                        currentDate.DayOfWeek !=
                            DayOfWeek.Sunday;
                }

                if (isWorkingDay)
                {
                    workingDates.Add(
                        currentDate
                    );
                }

                currentDate =
                    currentDate.AddDays(1);
            }

            return workingDates;
        }

        [HttpGet]
        public IActionResult ResetEmployeePassword(int id)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var adminId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Admin" || adminId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 管理者本人は通常のパスワード変更画面を使用する
            if (id == adminId.Value)
            {
                TempData["ErrorMessage"] =
                    "自分のパスワードは、画面上部の" +
                    "「パスワード変更」から変更してください。";

                return RedirectToAction("Employees");
            }

            var employee = _context.Employees
                .AsNoTracking()
                .FirstOrDefault(e =>
                    e.EmployeeId == id);

            if (employee == null)
            {
                _operationLogService.Write(
                    actionName: "パスワード再発行",
                    targetType: "Employee",
                    targetId: id,
                    details:
                        $"対象の社員が見つかりません。" +
                        $"社員ID：{id}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象の社員が見つかりません。";

                return RedirectToAction("Employees");
            }

            var viewModel =
                new EmployeePasswordResetViewModel
                {
                    EmployeeId =
                        employee.EmployeeId,

                    EmployeeName =
                        employee.Name,

                    Email =
                        employee.Email
                };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetEmployeePassword(
            EmployeePasswordResetViewModel viewModel)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var adminId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Admin" || adminId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (viewModel.EmployeeId == adminId.Value)
            {
                TempData["ErrorMessage"] =
                    "自分のパスワードは、画面上部の" +
                    "「パスワード変更」から変更してください。";

                return RedirectToAction("Employees");
            }

            var employee = _context.Employees
                .FirstOrDefault(e =>
                    e.EmployeeId == viewModel.EmployeeId);

            if (employee == null)
            {
                _operationLogService.Write(
                    actionName: "パスワード再発行",
                    targetType: "Employee",
                    targetId: viewModel.EmployeeId,
                    details:
                        $"対象の社員が見つかりません。" +
                        $"社員ID：{viewModel.EmployeeId}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象の社員が見つかりません。";

                return RedirectToAction("Employees");
            }

            // 入力エラーで画面を再表示するために再設定
            viewModel.EmployeeName =
                employee.Name;

            viewModel.Email =
                employee.Email;

            if (!ModelState.IsValid)
            {
                _operationLogService.Write(
                    actionName: "パスワード再発行",
                    targetType: "Employee",
                    targetId: employee.EmployeeId,
                    details:
                        "入力内容にエラーがあるため、" +
                        "パスワード再発行を中止しました。",
                    result: "失敗"
                );

                return View(viewModel);
            }

            var isSameAsCurrentPassword =
                PasswordHelper.VerifyPassword(
                    viewModel.TemporaryPassword,
                    employee.PasswordHash
                );

            if (isSameAsCurrentPassword)
            {
                ModelState.AddModelError(
                    nameof(viewModel.TemporaryPassword),
                    "現在のパスワードと異なる" +
                    "仮パスワードを設定してください。"
                );

                _operationLogService.Write(
                    actionName: "パスワード再発行",
                    targetType: "Employee",
                    targetId: employee.EmployeeId,
                    details:
                        "現在と同じパスワードが指定されたため、" +
                        "パスワード再発行を拒否しました。",
                    result: "失敗"
                );

                return View(viewModel);
            }

            employee.PasswordHash =
                PasswordHelper.HashPassword(
                    viewModel.TemporaryPassword
                );

            // 次回ログイン時に本人による変更を必須にする
            employee.MustChangePassword = true;

            // ロック状態も解除する
            employee.FailedLoginCount = 0;
            employee.LastFailedLoginAt = null;
            employee.LockoutEndAt = null;
            employee.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            _operationLogService.Write(
                actionName: "パスワード再発行",
                targetType: "Employee",
                targetId: employee.EmployeeId,
                details:
                    $"社員の仮パスワードを再発行しました。" +
                    $"氏名：{employee.Name}、" +
                    $"メールアドレス：{employee.Email}、" +
                    $"次回ログイン時のパスワード変更：必須。",
                result: "成功"
            );

            TempData["SuccessMessage"] =
                $"{employee.Name}さんの仮パスワードを" +
                "再発行しました。";

            return RedirectToAction("Employees");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnlockEmployee(int id)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = _context.Employees
                .FirstOrDefault(e => e.EmployeeId == id);

            if (employee == null)
            {
                _operationLogService.Write(
                    actionName: "ロック解除",
                    targetType: "Employee",
                    targetId: id,
                    details:
                        $"対象の社員が見つかりません。" +
                        $"社員ID：{id}。",
                    result: "失敗"
                );

                TempData["ErrorMessage"] =
                    "対象の社員が見つかりません。";

                return RedirectToAction("Employees");
            }

            var wasLocked =
                employee.LockoutEndAt.HasValue &&
                employee.LockoutEndAt.Value > DateTime.Now;

            var beforeFailedLoginCount =
                employee.FailedLoginCount;

            var beforeLockoutEndAt =
                employee.LockoutEndAt;

            employee.FailedLoginCount = 0;
            employee.LastFailedLoginAt = null;
            employee.LockoutEndAt = null;
            employee.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            var beforeLockoutText =
                beforeLockoutEndAt.HasValue
                    ? beforeLockoutEndAt.Value
                        .ToString("yyyy/MM/dd HH:mm:ss")
                    : "-";

            _operationLogService.Write(
                actionName: "ロック解除",
                targetType: "Employee",
                targetId: employee.EmployeeId,
                details:
                    $"社員アカウントのロック情報を解除しました。" +
                    $"氏名：{employee.Name}、" +
                    $"ロック状態：" +
                    $"{(wasLocked ? "ロック中" : "通常")}、" +
                    $"失敗回数：{beforeFailedLoginCount}回、" +
                    $"ロック終了予定：{beforeLockoutText}。",
                result: "成功"
            );

            TempData["SuccessMessage"] =
                $"{employee.Name}さんのアカウントロックを" +
                "解除しました。";

            return RedirectToAction("Employees");
        }

        public IActionResult AttendanceStampLogs(
    DateTime? startDate,
    DateTime? endDate,
    string? stampType,
    string? gpsStatus,
    string? keyword,
    int page = 1)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            const int pageSize = 20;

            if (page < 1)
            {
                page = 1;
            }

            var query = _context.AttendanceStampLogs
                .AsNoTracking()
                .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
                .AsQueryable();

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;

                query = query.Where(l =>
                    l.StampedAt >= start);
            }

            if (endDate.HasValue)
            {
                var endExclusive =
                    endDate.Value.Date.AddDays(1);

                query = query.Where(l =>
                    l.StampedAt < endExclusive);
            }

            if (!string.IsNullOrWhiteSpace(stampType))
            {
                query = query.Where(l =>
                    l.StampType == stampType);
            }

            if (!string.IsNullOrWhiteSpace(gpsStatus))
            {
                query = query.Where(l =>
                    l.GpsStatus == gpsStatus);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var searchKeyword = keyword.Trim();

                query = query.Where(l =>
                    (l.Employee != null &&
                     (l.Employee.Name.Contains(searchKeyword) ||
                      l.Employee.Email.Contains(searchKeyword))) ||
                    l.IpAddress.Contains(searchKeyword) ||
                    l.DeviceType.Contains(searchKeyword) ||
                    l.Details.Contains(searchKeyword));
            }

            var totalCount = query.Count();

            var totalPages =
                totalCount == 0
                    ? 1
                    : (int)Math.Ceiling(
                        (double)totalCount / pageSize);

            if (page > totalPages)
            {
                page = totalPages;
            }

            var logs = query
                .OrderByDescending(l => l.StampedAt)
                .ThenByDescending(l =>
                    l.AttendanceStampLogId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l =>
                    new AdminAttendanceStampLogItemViewModel
                    {
                        AttendanceStampLogId =
                            l.AttendanceStampLogId,

                        AttendanceId =
                            l.AttendanceId,

                        EmployeeId =
                            l.EmployeeId,

                        EmployeeName =
                            l.Employee != null
                                ? l.Employee.Name
                                : "",

                        DepartmentName =
                            l.Employee != null &&
                            l.Employee.Department != null
                                ? l.Employee.Department.DepartmentName
                                : "",

                        StampType =
                            l.StampType,

                        StampedAt =
                            l.StampedAt,

                        Latitude =
                            l.Latitude,

                        Longitude =
                            l.Longitude,

                        AccuracyMeters =
                            l.AccuracyMeters,

                        GpsStatus =
                            l.GpsStatus,

                        DeviceType =
                            l.DeviceType,

                        IpAddress =
                            l.IpAddress,

                        Result =
                            l.Result,

                        Details =
                            l.Details,

                        UserAgent =
                            l.UserAgent
                    })
                .ToList();

            var viewModel =
                new AdminAttendanceStampLogViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    StampType = stampType,
                    GpsStatus = gpsStatus,
                    Keyword = keyword,
                    Logs = logs,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                };

            return View(viewModel);
        }


        public IActionResult OperationLogs(
            DateTime? startDate,
            DateTime? endDate,
            string? actionName,
            string? result,
            string? keyword,
            int page = 1)
        {
            var role = HttpContext.Session.GetString("LoginUserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            const int pageSize = 20;

            if (page < 1)
            {
                page = 1;
            }

            var query = _context.OperationLogs
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;

                query = query.Where(l =>
                    l.CreatedAt >= start);
            }

            if (endDate.HasValue)
            {
                var endExclusive =
                    endDate.Value.Date.AddDays(1);

                query = query.Where(l =>
                    l.CreatedAt < endExclusive);
            }

            if (!string.IsNullOrWhiteSpace(actionName))
            {
                query = query.Where(l =>
                    l.ActionName == actionName);
            }

            if (!string.IsNullOrWhiteSpace(result))
            {
                query = query.Where(l =>
                    l.Result == result);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var searchKeyword = keyword.Trim();

                query = query.Where(l =>
                    l.UserName.Contains(searchKeyword) ||
                    l.Details.Contains(searchKeyword) ||
                    l.IpAddress.Contains(searchKeyword) ||
                    l.DeviceType.Contains(searchKeyword));
            }

            var totalCount = query.Count();

            var totalPages =
                totalCount == 0
                    ? 1
                    : (int)Math.Ceiling(
                        (double)totalCount / pageSize);

            if (page > totalPages)
            {
                page = totalPages;
            }

            var logs = query
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.OperationLogId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l =>
                    new AdminOperationLogItemViewModel
                    {
                        OperationLogId = l.OperationLogId,
                        EmployeeId = l.EmployeeId,
                        UserName = l.UserName,
                        Role = l.Role,
                        ActionName = l.ActionName,
                        TargetType = l.TargetType,
                        TargetId = l.TargetId,
                        Details = l.Details,
                        Result = l.Result,
                        IpAddress = l.IpAddress,
                        DeviceType = l.DeviceType,
                        UserAgent = l.UserAgent,
                        CreatedAt = l.CreatedAt
                    })
                .ToList();

            var actionNames = _context.OperationLogs
                .AsNoTracking()
                .Where(l => l.ActionName != "")
                .Select(l => l.ActionName)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            var viewModel = new AdminOperationLogViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                ActionName = actionName,
                Result = result,
                Keyword = keyword,
                ActionNames = actionNames,
                Logs = logs,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        private static bool TryParseYearMonth(
            string? yearMonth,
            out DateTime targetMonth)
        {
            if (string.IsNullOrWhiteSpace(yearMonth))
            {
                targetMonth = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1
                );

                return true;
            }

            var parsed = DateTime.TryParseExact(
                yearMonth,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedMonth
            );

            if (!parsed ||
                parsedMonth.Year < 2000 ||
                parsedMonth.Year > 2100)
            {
                targetMonth = default;
                return false;
            }

            targetMonth = new DateTime(
                parsedMonth.Year,
                parsedMonth.Month,
                1
            );

            return true;
        }

        [HttpGet]
        public async Task<IActionResult> ExportAttendanceCsv(
           string? yearMonth,
           int? departmentId,
           string? keyword)
        {
            var role =
                HttpContext.Session.GetString(
                    "LoginUserRole"
                );

            if (role != "Admin")
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var targetMonth =
                DateTime.Today;

            if (!string.IsNullOrWhiteSpace(
                    yearMonth))
            {
                if (DateTime.TryParse(
                        yearMonth + "-01",
                        out var parsedMonth))
                {
                    targetMonth =
                        parsedMonth;
                }
            }

            var startDate =
                new DateTime(
                    targetMonth.Year,
                    targetMonth.Month,
                    1
                );

            var endDate =
                startDate.AddMonths(1);

            var today =
                DateTime.Today;

            // =====================================
            // 会社カレンダー
            // =====================================

            var companyCalendarDays =
                await _companyCalendarService
                    .GetMonthAsync(
                        startDate.Year,
                        startDate.Month
                    );

            var calendarMap =
                companyCalendarDays
                    .GroupBy(c =>
                        c.CalendarDate.Date)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.First().IsWorkingDay
                    );

            var companyWorkingDates =
                new HashSet<DateTime>();

            for (var date = startDate;
                 date < endDate;
                 date = date.AddDays(1))
            {
                bool isWorkingDay;

                if (calendarMap.TryGetValue(
                        date.Date,
                        out var calendarWorkingDay))
                {
                    isWorkingDay =
                        calendarWorkingDay;
                }
                else
                {
                    isWorkingDay =
                        date.DayOfWeek !=
                            DayOfWeek.Saturday &&
                        date.DayOfWeek !=
                            DayOfWeek.Sunday;
                }

                if (isWorkingDay)
                {
                    companyWorkingDates.Add(
                        date.Date
                    );
                }
            }

            // =====================================
            // 対象社員
            // =====================================

            var employeesQuery =
                _context.Employees
                    .AsNoTracking()
                    .Include(e =>
                        e.Department)
                    .Where(e =>
                        e.IsActive &&
                        e.Role == "Employee" &&
                        e.JoinDate < endDate)
                    .AsQueryable();

            if (departmentId.HasValue &&
                departmentId.Value > 0)
            {
                employeesQuery =
                    employeesQuery.Where(e =>
                        e.DepartmentId ==
                        departmentId.Value
                    );
            }

            if (!string.IsNullOrWhiteSpace(
                    keyword))
            {
                var searchKeyword =
                    keyword.Trim();

                employeesQuery =
                    employeesQuery.Where(e =>
                        e.Name.Contains(
                            searchKeyword
                        ) ||
                        e.Email.Contains(
                            searchKeyword
                        )
                    );
            }

            var employees =
                await employeesQuery
                    .OrderBy(e =>
                        e.EmployeeId)
                    .ToListAsync();

            var employeeIds =
                employees
                    .Select(e =>
                        e.EmployeeId)
                    .ToList();

            // =====================================
            // 勤怠・有給データ
            // =====================================

            var attendances =
                await _context.Attendances
                    .AsNoTracking()
                    .Where(a =>
                        employeeIds.Contains(
                            a.EmployeeId
                        ) &&
                        a.AttendanceDate >=
                            startDate &&
                        a.AttendanceDate <
                            endDate)
                    .ToListAsync();

            var approvedPaidLeaves =
                await _context.PaidLeaveRequests
                    .AsNoTracking()
                    .Where(p =>
                        employeeIds.Contains(
                            p.EmployeeId
                        ) &&
                        p.Status == "承認" &&
                        p.LeaveDate >=
                            startDate &&
                        p.LeaveDate <
                            endDate)
                    .ToListAsync();

            var confirmerIds =
                attendances
                    .Where(a =>
                        a.AbsenceConfirmedBy
                            .HasValue)
                    .Select(a =>
                        a.AbsenceConfirmedBy
                            .Value)
                    .Distinct()
                    .ToList();

            var confirmerNames =
                await _context.Employees
                    .AsNoTracking()
                    .Where(e =>
                        confirmerIds.Contains(
                            e.EmployeeId
                        ))
                    .ToDictionaryAsync(
                        e =>
                            e.EmployeeId,
                        e =>
                            e.Name
                    );

            // =====================================
            // CSV行データ作成
            // =====================================

            var rows =
                new List<(
                    DateTime AttendanceDate,
                    int EmployeeId,
                    string EmployeeName,
                    string DepartmentName,
                    TimeSpan? ClockInTime,
                    TimeSpan? ClockOutTime,
                    int BreakMinutes,
                    int WorkMinutes,
                    int LateMinutes,
                    int OvertimeMinutes,
                    string Status,
                    string AbsenceReason,
                    string AbsenceConfirmedByName,
                    DateTime? AbsenceConfirmedAt
                )>();

            foreach (var employee in employees)
            {
                var employeeStartDate =
                    employee.JoinDate.Date >
                    startDate
                        ? employee.JoinDate.Date
                        : startDate;

                var employeeAttendances =
                    attendances
                        .Where(a =>
                            a.EmployeeId ==
                            employee.EmployeeId)
                        .ToList();

                var employeePaidLeaves =
                    approvedPaidLeaves
                        .Where(p =>
                            p.EmployeeId ==
                            employee.EmployeeId)
                        .ToList();

                var displayDates =
                    new HashSet<DateTime>();

                // 所定労働日は当日まで表示する
                foreach (var workingDate
                         in companyWorkingDates)
                {
                    if (workingDate >=
                            employeeStartDate &&
                        workingDate <=
                            today &&
                        workingDate <
                            endDate)
                    {
                        displayDates.Add(
                            workingDate
                        );
                    }
                }

                // 実際の勤怠レコードがある日は表示する
                foreach (var attendance
                         in employeeAttendances)
                {
                    if (attendance.AttendanceDate.Date >=
                            employeeStartDate &&
                        attendance.AttendanceDate.Date <
                            endDate)
                    {
                        displayDates.Add(
                            attendance.AttendanceDate.Date
                        );
                    }
                }

                // 承認済み有給は未来日も表示する
                foreach (var paidLeave
                         in employeePaidLeaves)
                {
                    if (paidLeave.LeaveDate.Date >=
                            employeeStartDate &&
                        paidLeave.LeaveDate.Date <
                            endDate)
                    {
                        displayDates.Add(
                            paidLeave.LeaveDate.Date
                        );
                    }
                }

                foreach (var displayDate
                         in displayDates)
                {
                    var attendance =
                        employeeAttendances
                            .FirstOrDefault(a =>
                                a.AttendanceDate.Date ==
                                displayDate
                            );

                    var hasApprovedPaidLeave =
                        employeePaidLeaves.Any(p =>
                            p.LeaveDate.Date ==
                            displayDate
                        );

                    var isCompanyWorkingDay =
                        companyWorkingDates.Contains(
                            displayDate
                        );

                    string status;

                    if (attendance?.IsAbsent == true)
                    {
                        status =
                            "欠勤";
                    }
                    else if (
                        hasApprovedPaidLeave &&
                        !(
                            attendance?.ClockInTime
                                .HasValue ??
                            false
                        ))
                    {
                        status =
                            displayDate > today
                                ? "有給予定"
                                : "有給";
                    }
                    else if (
                        attendance?.ClockInTime
                            .HasValue ==
                        true)
                    {
                        status =
                            attendance.Status;
                    }
                    else if (
                        isCompanyWorkingDay &&
                        displayDate <= today)
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

                    rows.Add(
                        (
                            AttendanceDate:
                                displayDate,

                            EmployeeId:
                                employee.EmployeeId,

                            EmployeeName:
                                employee.Name,

                            DepartmentName:
                                employee.Department
                                    ?.DepartmentName ??
                                string.Empty,

                            ClockInTime:
                                attendance?.ClockInTime,

                            ClockOutTime:
                                attendance?.ClockOutTime,

                            BreakMinutes:
                                attendance
                                    ?.BreakMinutes ??
                                0,

                            WorkMinutes:
                                attendance
                                    ?.WorkMinutes ??
                                0,

                            LateMinutes:
                                attendance
                                    ?.LateMinutes ??
                                0,

                            OvertimeMinutes:
                                attendance
                                    ?.OvertimeMinutes ??
                                0,

                            Status:
                                status,

                            AbsenceReason:
                                attendance
                                    ?.AbsenceReason ??
                                string.Empty,

                            AbsenceConfirmedByName:
                                confirmedByName ??
                                string.Empty,

                            AbsenceConfirmedAt:
                                attendance
                                    ?.AbsenceConfirmedAt
                        )
                    );
                }
            }

            // =====================================
            // CSV作成
            // =====================================

            static string EscapeCsv(
                string? value)
            {
                var text =
                    value ?? string.Empty;

                return
                    "\"" +
                    text.Replace(
                        "\"",
                        "\"\""
                    ) +
                    "\"";
            }

            static string FormatTime(
                TimeSpan? time)
            {
                return time.HasValue
                    ? time.Value.ToString(
                        @"hh\:mm"
                    )
                    : string.Empty;
            }

            static string FormatMinutes(
                int minutes)
            {
                var safeMinutes =
                    Math.Max(
                        0,
                        minutes
                    );

                return
                    $"{safeMinutes / 60:D2}:" +
                    $"{safeMinutes % 60:D2}";
            }

            var csv =
                new System.Text.StringBuilder();

            csv.AppendLine(
                string.Join(
                    ",",
                    new[]
                    {
                        EscapeCsv("日付"),
                        EscapeCsv("社員ID"),
                        EscapeCsv("社員名"),
                        EscapeCsv("部署"),
                        EscapeCsv("出勤"),
                        EscapeCsv("退勤"),
                        EscapeCsv("休憩分"),
                        EscapeCsv("実働時間"),
                        EscapeCsv("遅刻時間"),
                        EscapeCsv("残業時間"),
                        EscapeCsv("状態"),
                        EscapeCsv("欠勤理由"),
                        EscapeCsv("欠勤確定者"),
                        EscapeCsv("欠勤確定日時")
                    }
                )
            );

            foreach (var row
                     in rows
                        .OrderByDescending(r =>
                            r.AttendanceDate)
                        .ThenBy(r =>
                            r.EmployeeId))
            {
                csv.AppendLine(
                    string.Join(
                        ",",
                        new[]
                        {
                            EscapeCsv(
                                row.AttendanceDate
                                    .ToString(
                                        "yyyy/MM/dd"
                                    )
                            ),

                            EscapeCsv(
                                row.EmployeeId
                                    .ToString()
                            ),

                            EscapeCsv(
                                row.EmployeeName
                            ),

                            EscapeCsv(
                                row.DepartmentName
                            ),

                            EscapeCsv(
                                FormatTime(
                                    row.ClockInTime
                                )
                            ),

                            EscapeCsv(
                                FormatTime(
                                    row.ClockOutTime
                                )
                            ),

                            EscapeCsv(
                                row.BreakMinutes
                                    .ToString()
                            ),

                            EscapeCsv(
                                FormatMinutes(
                                    row.WorkMinutes
                                )
                            ),

                            EscapeCsv(
                                FormatMinutes(
                                    row.LateMinutes
                                )
                            ),

                            EscapeCsv(
                                FormatMinutes(
                                    row.OvertimeMinutes
                                )
                            ),

                            EscapeCsv(
                                row.Status
                            ),

                            EscapeCsv(
                                row.AbsenceReason
                            ),

                            EscapeCsv(
                                row.AbsenceConfirmedByName
                            ),

                            EscapeCsv(
                                row.AbsenceConfirmedAt
                                    ?.ToString(
                                        "yyyy/MM/dd HH:mm"
                                    ) ??
                                string.Empty
                            )
                        }
                    )
                );
            }

            _operationLogService.Write(
                actionName:
                    "勤怠CSV出力",

                targetType:
                    "Attendance",

                details:
                    $"勤怠一覧CSVを出力しました。" +
                    $"対象年月：{startDate:yyyy年MM月}、" +
                    $"部署ID：" +
                    $"{departmentId?.ToString() ?? "すべて"}、" +
                    $"検索キーワード：" +
                    $"{keyword ?? "なし"}、" +
                    $"出力件数：{rows.Count}件。",

                result:
                    "成功"
            );

            var contentBytes =
                System.Text.Encoding.UTF8
                    .GetBytes(
                        csv.ToString()
                    );

            var bom =
                new byte[]
                {
                    0xEF,
                    0xBB,
                    0xBF
                };

            var fileBytes =
                bom
                    .Concat(
                        contentBytes
                    )
                    .ToArray();

            var fileName =
                $"勤怠一覧_" +
                $"{startDate:yyyyMM}.csv";

            return File(
                fileBytes,
                "text/csv; charset=utf-8",
                fileName
            );
        }


        private List<DepartmentSelectViewModel>
            GetDepartments()
        {
            return _context.Departments
                .OrderBy(d => d.DepartmentId)
                .Select(d =>
                    new DepartmentSelectViewModel
                    {
                        DepartmentId = d.DepartmentId,
                        DepartmentName = d.DepartmentName
                    })
                .ToList();
        }
    }
}