using Microsoft.EntityFrameworkCore;
using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using AttendanceManagementSystem.Services;
using AttendanceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagementSystem.Controllers
{
    public class PaidLeaveController : Controller
    {
        private readonly ApplicationDbContext
            _context;

        private readonly PaidLeaveRuleService
            _paidLeaveRuleService;

        private readonly OperationLogService
            _operationLogService;

        private readonly MonthlyClosingService
            _monthlyClosingService;

        private readonly CompanyCalendarService
            _companyCalendarService;

        private readonly PaidLeaveGrantHistoryService
            _paidLeaveGrantHistoryService;

        private readonly PaidLeaveBalanceCalculationService
            _paidLeaveBalanceCalculationService;

        public PaidLeaveController(
            ApplicationDbContext context,
            PaidLeaveRuleService paidLeaveRuleService,
            OperationLogService operationLogService,
            MonthlyClosingService monthlyClosingService,
            CompanyCalendarService companyCalendarService,
            PaidLeaveGrantHistoryService paidLeaveGrantHistoryService,
            PaidLeaveBalanceCalculationService paidLeaveBalanceCalculationService)
        {
            _context =
                context;

            _paidLeaveRuleService =
                paidLeaveRuleService;

            _operationLogService =
                operationLogService;

            _monthlyClosingService =
                monthlyClosingService;

            _companyCalendarService =
                companyCalendarService;

            _paidLeaveGrantHistoryService =
                paidLeaveGrantHistoryService;

            _paidLeaveBalanceCalculationService =
                paidLeaveBalanceCalculationService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("LoginUserRole");
            var employeeId = HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Employee" || employeeId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = _context.Employees
                .FirstOrDefault(e => e.EmployeeId == employeeId.Value);

            if (employee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var balance = SynchronizePaidLeaveBalance(employee);

            var viewModel = new PaidLeaveCreateViewModel
            {
                RemainingDays = balance.RemainingDays,
                Message = TempData["Message"] as string
            };

            LoadNonWorkingDays(viewModel);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PaidLeaveCreateViewModel viewModel)
        {
            var role = HttpContext.Session.GetString("LoginUserRole");
            var employeeId = HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Employee" || employeeId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = _context.Employees
                .FirstOrDefault(e => e.EmployeeId == employeeId.Value);

            if (employee == null)
            {
                _operationLogService.Write(
                    actionName: "有給申請",
                    targetType: "Employee",
                    targetId: employeeId.Value,
                    details: "社員情報が見つからないため、有給申請を拒否しました。",
                    result: "失敗"
                );

                return RedirectToAction("Login", "Account");
            }

            var balance = SynchronizePaidLeaveBalance(employee);

            viewModel.RemainingDays = balance.RemainingDays;
            viewModel.LeaveDate = viewModel.LeaveDate.Date;
            LoadNonWorkingDays(viewModel);

            if (!ModelState.IsValid)
            {
                _operationLogService.Write(
                    actionName: "有給申請",
                    targetType: "PaidLeaveRequest",
                    details:
                        $"入力内容にエラーがあります。取得日：{viewModel.LeaveDate:yyyy/MM/dd}、申請日数：{viewModel.Days:0.#}日。",
                    result: "失敗"
                );

                return View(viewModel);
            }
            // 会社カレンダー上の所定労働日か確認する
            var isCompanyWorkingDay =
                await _companyCalendarService
                    .IsWorkingDayAsync(
                        viewModel.LeaveDate
                    );

            if (!isCompanyWorkingDay)
            {
                var calendarDay =
                    await _context.CompanyCalendarDays
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c =>
                            c.CalendarDate ==
                            viewModel.LeaveDate
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

                ModelState.AddModelError(
                    nameof(viewModel.LeaveDate),
                    $"{viewModel.LeaveDate:yyyy/MM/dd}は" +
                    $"{holidayText}のため、有給申請できません。"
                );

                _operationLogService.Write(
                    actionName: "有給申請",
                    targetType: "CompanyCalendarDay",
                    targetId:
                        calendarDay?.CompanyCalendarDayId,
                    details:
                        $"会社カレンダー上の休日のため、" +
                        $"有給申請を拒否しました。" +
                        $"取得日：{viewModel.LeaveDate:yyyy/MM/dd}、" +
                        $"日付区分：{holidayText}、" +
                        $"申請日数：{viewModel.Days:0.#}日。",
                    result: "失敗"
                );

                return View(viewModel);
            }

            if (_monthlyClosingService.IsClosed(viewModel.LeaveDate))
            {
                var closedMonthText =
                    viewModel.LeaveDate.ToString("yyyy年MM月");

                _operationLogService.Write(
                    actionName: "有給申請",
                    targetType: "PaidLeaveRequest",
                    details:
                        $"月次締め済みのため、有給申請を拒否しました。" +
                        $"取得日：{viewModel.LeaveDate:yyyy/MM/dd}、" +
                        $"申請日数：{viewModel.Days:0.#}日。",
                    result: "失敗"
                );

                ModelState.AddModelError(
                    "LeaveDate",
                    $"{closedMonthText}は月次締め済みのため、有給申請できません。"
                );

                return View(viewModel);
            }

            if (viewModel.Days <= 0)
            {
                ModelState.AddModelError(
                    "Days",
                    "日数を正しく入力してください。"
                );

                _operationLogService.Write(
                    actionName: "有給申請",
                    targetType: "PaidLeaveRequest",
                    details:
                        $"申請日数が不正です。取得日：{viewModel.LeaveDate:yyyy/MM/dd}、申請日数：{viewModel.Days:0.#}日。",
                    result: "失敗"
                );

                return View(viewModel);
            }

            if (balance.RemainingDays <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "現在、有給残日数がありません。"
                );

                _operationLogService.Write(
                    actionName: "有給申請",
                    targetType: "PaidLeaveBalance",
                    details:
                        $"有給残日数が0日のため申請を拒否しました。取得日：{viewModel.LeaveDate:yyyy/MM/dd}。",
                    result: "失敗"
                );

                return View(viewModel);
            }

            if (viewModel.Days > balance.RemainingDays)
            {
                ModelState.AddModelError(
                    "Days",
                    "有給残日数が不足しています。"
                );

                _operationLogService.Write(
                    actionName: "有給申請",
                    targetType: "PaidLeaveBalance",
                    details:
                        $"有給残日数不足のため申請を拒否しました。残日数：{balance.RemainingDays:0.#}日、申請日数：{viewModel.Days:0.#}日。",
                    result: "失敗"
                );

                return View(viewModel);
            }

            var duplicateRequest = _context.PaidLeaveRequests
                .Any(p =>
                    p.EmployeeId == employeeId.Value &&
                    p.LeaveDate == viewModel.LeaveDate &&
                    (p.Status == "申請中" || p.Status == "承認"));

            if (duplicateRequest)
            {
                ModelState.AddModelError(
                    "LeaveDate",
                    "同じ取得日の有給申請がすでに登録されています。"
                );

                _operationLogService.Write(
                    actionName: "有給申請",
                    targetType: "PaidLeaveRequest",
                    details:
                        $"同じ取得日の重複申請を拒否しました。取得日：{viewModel.LeaveDate:yyyy/MM/dd}。",
                    result: "失敗"
                );

                return View(viewModel);
            }

            var request = new PaidLeaveRequest
            {
                EmployeeId = employeeId.Value,
                LeaveDate = viewModel.LeaveDate,
                Days = viewModel.Days,
                Reason = viewModel.Reason,
                Status = "申請中",
                CreatedAt = DateTime.Now
            };

            _context.PaidLeaveRequests.Add(request);
            _context.SaveChanges();

            _operationLogService.Write(
                actionName: "有給申請",
                targetType: "PaidLeaveRequest",
                targetId: request.PaidLeaveRequestId,
                details:
                    $"有給申請を送信しました。取得日：{request.LeaveDate:yyyy/MM/dd}、申請日数：{request.Days:0.#}日、申請時残日数：{balance.RemainingDays:0.#}日。",
                result: "成功"
            );

            TempData["Message"] = "有給申請を送信しました。";

            return RedirectToAction("Create");
        }

        public IActionResult MyBalance()
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
                    e.EmployeeId == employeeId.Value);

            if (employee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var balance =
                SynchronizePaidLeaveBalance(employee);

            var legalResult =
                CalculatePaidLeaveRule(employee);

            var grantHistories =
                _paidLeaveGrantHistoryService
                    .GetEmployeeGrantHistories(
                        employee.EmployeeId
                    );

            var requests =
                _context.PaidLeaveRequests
                    .Where(p =>
                        p.EmployeeId ==
                        employeeId.Value)
                    .OrderByDescending(p =>
                        p.CreatedAt)
                    .ToList();

            var approverIds = requests
                .Where(r => r.ApprovedBy.HasValue)
                .Select(r => r.ApprovedBy!.Value)
                .Distinct()
                .ToList();

            var approverNames = _context.Employees
                .Where(e =>
                    approverIds.Contains(e.EmployeeId))
                .ToDictionary(
                    e => e.EmployeeId,
                    e => e.Name
                );

            var viewModel = new PaidLeaveBalanceViewModel
            {
                GrantedDays =
    balance.GrantedDays,

                UsedDays =
    balance.UsedDays,

                ReservedDays =
    balance.ReservedDays,

                RemainingDays =
    balance.RemainingDays,

                CurrentGrantedDays =
                    balance.CurrentGrantedDays,

                CarriedOverDays =
                    balance.CarriedOverDays,

                ExpiredDays =
                    balance.ExpiredDays,

                CurrentGrantExpiryDate =
                    balance.CurrentGrantExpiryDate,

                LastCalculatedAt =
                    balance.LastCalculatedAt,

                RequiredDays =
                    legalResult.RemainingFiveDayRequirement,

                JoinDate = legalResult.JoinDate,
                CurrentGrantDate =
                    legalResult.CurrentGrantDate,
                NextGrantDate =
                    legalResult.NextGrantDate,
                FiveDayDeadline =
                    legalResult.FiveDayDeadline,
                LegalExpiryDate =
                    legalResult.LegalExpiryDate,
                LegalGrantedDays =
                    legalResult.GrantedDays,

                TotalWorkDaysInCheckPeriod =
                    legalResult.TotalWorkDaysInCheckPeriod,

                AttendedDaysInCheckPeriod =
                    legalResult.AttendedDaysInCheckPeriod,

                AttendanceRate =
                    legalResult.AttendanceRate,

                IsAttendanceRateEnough =
                    legalResult.IsAttendanceRateEnough,

                UsedDaysAfterCurrentGrant =
                    legalResult.UsedDaysAfterCurrentGrant,

                RemainingFiveDayRequirement =
                    legalResult.RemainingFiveDayRequirement,

                IsFiveDayAlertTarget =
                    legalResult.IsFiveDayAlertTarget,

                LegalMessage =
                    legalResult.Message,

                GrantHistories =
    grantHistories
        .Select(history =>
            new PaidLeaveGrantHistoryItemViewModel
            {
                PaidLeaveGrantHistoryId =
                    history.PaidLeaveGrantHistoryId,

                GrantDate =
                    history.GrantDate,

                AttendanceCheckStartDate =
                    history.AttendanceCheckStartDate,

                AttendanceCheckEndDate =
                    history.AttendanceCheckEndDate,

                TotalWorkDays =
                    history.TotalWorkDays,

                AttendedDays =
                    history.AttendedDays,

                AttendanceRate =
                    history.AttendanceRate,

                IsAttendanceRateEnough =
                    history.IsAttendanceRateEnough,

                GrantStatus =
                    history.GrantStatus,

                GrantedDays =
                    history.GrantedDays,

                UsedDays =
                    history.UsedDays,

                RemainingDays =
                    history.RemainingDays,

                ExpiryDate =
                    history.ExpiryDate,

                DecisionReason =
                    history.DecisionReason ??
                    string.Empty,

                CreatedAt =
                    history.CreatedAt,

                UpdatedAt =
                    history.UpdatedAt
            })
        .ToList(),

                Requests =
    requests
                    .Select(r =>
                        new PaidLeaveHistoryItemViewModel
                        {
                            PaidLeaveRequestId =
                                r.PaidLeaveRequestId,

                            LeaveDate =
                                r.LeaveDate,

                            Days =
                                r.Days,

                            Reason =
                                r.Reason ?? string.Empty,

                            Status =
                                r.Status,

                            CreatedAt =
                                r.CreatedAt,

                            ApprovedAt =
                                r.ApprovedAt,

                            ApprovedBy =
                                r.ApprovedBy,

                            ApprovedByName =
                                r.ApprovedBy.HasValue &&
                                approverNames.TryGetValue(
                                    r.ApprovedBy.Value,
                                    out var approverName)
                                        ? approverName
                                        : "-",

                            AdminComment =
                                r.AdminComment ?? string.Empty
                        })
                    .ToList()
            };

            return View(viewModel);
        }

        private PaidLeaveBalance SynchronizePaidLeaveBalance(
            Employee employee)
        {
            var targetDate =
                DateTime.Today;

            var now =
                DateTime.Now;

            var year =
                targetDate.Year;

            // 入社後に到来したすべての付与日について、
            // 出勤率判定と有給付与履歴を同期する
            SynchronizeAllGrantHistories(
                employee,
                targetDate
            );

            // 付与履歴と承認済み有給を基準に
            // 繰越・失効・残日数を再計算する
            var calculationResult =
                _paidLeaveBalanceCalculationService
                    .Calculate(
                        employee.EmployeeId,
                        targetDate
                    );

            var balance =
                _context.PaidLeaveBalances
                    .FirstOrDefault(p =>
                        p.EmployeeId ==
                            employee.EmployeeId &&
                        p.Year ==
                            year
                    );

            if (balance == null)
            {
                balance =
                    new PaidLeaveBalance
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        Year =
                            year,

                        CreatedAt =
                            now
                    };

                _context.PaidLeaveBalances
                    .Add(balance);
            }

            // 当期付与分
            balance.CurrentGrantedDays =
                calculationResult.CurrentGrantedDays;

            // 前回付与分からの有効な繰越
            balance.CarriedOverDays =
                calculationResult.CarriedOverDays;

            // 2年の有効期限を過ぎた失効分
            balance.ExpiredDays =
                calculationResult.ExpiredDays;

            // 当期付与分＋有効な繰越分
            balance.GrantedDays =
                calculationResult.GrantedDays;

            // 現在の付与日以降の使用日数
            balance.UsedDays =
                calculationResult.UsedDays;
            // 承認済みの将来利用予定日数
            balance.ReservedDays =
                calculationResult.ReservedDays;

            // 現在利用可能な残日数
            balance.RemainingDays =
                calculationResult.RemainingDays;

            balance.CurrentGrantDate =
                calculationResult.CurrentGrantDate;

            balance.CurrentGrantExpiryDate =
                calculationResult.CurrentGrantExpiryDate;

            balance.LastCalculatedAt =
                now;

            balance.UpdatedAt =
                now;

            _context.SaveChanges();

            return balance;
        }

        /// <summary>
        /// 入社後に到来したすべての有給付与日について、
        /// 出勤率判定と付与履歴を登録または更新する。
        /// </summary>
        private void SynchronizeAllGrantHistories(
            Employee employee,
            DateTime targetDate)
        {
            targetDate =
                targetDate.Date;

            var grantPeriods =
                _paidLeaveRuleService
                    .GetGrantPeriods(
                        employee.JoinDate,
                        targetDate
                    );

            foreach (var grantPeriod
                     in grantPeriods)
            {
                var ruleResult =
                    CalculatePaidLeaveRuleForGrantPeriod(
                        employee,
                        grantPeriod
                    );

                _paidLeaveGrantHistoryService
                    .SynchronizeCurrentGrantHistory(
                        employee,
                        ruleResult
                    );
            }

            // 履歴作成後、実際に取得済みの有給を
            // 有効期限が近い古い付与分から順に割り当てる
            UpdateGrantHistoryUsage(
                employee.EmployeeId,
                targetDate
            );
        }

        /// <summary>
        /// 指定された付与期間の出勤率と
        /// 法定付与日数を計算する。
        /// </summary>
        private PaidLeaveRuleResult
            CalculatePaidLeaveRuleForGrantPeriod(
                Employee employee,
                PaidLeaveGrantPeriod grantPeriod)
        {
            var checkStartDate =
                grantPeriod
                    .AttendanceCheckStartDate
                    .Date;

            var checkEndDate =
                grantPeriod
                    .AttendanceCheckEndDate
                    .Date;

            var checkEndDateExclusive =
                checkEndDate.AddDays(1);

            var companyWorkingDates =
                GetCompanyWorkingDates(
                    checkStartDate,
                    checkEndDate
                );

            var totalWorkDays =
                companyWorkingDates.Count;

            var actualAttendanceDates =
                _context.Attendances
                    .AsNoTracking()
                    .Where(attendance =>
                        attendance.EmployeeId ==
                            employee.EmployeeId &&
                        attendance.AttendanceDate >=
                            checkStartDate &&
                        attendance.AttendanceDate <
                            checkEndDateExclusive &&
                        attendance.ClockInTime.HasValue)
                    .Select(attendance =>
                        attendance.AttendanceDate)
                    .ToList()
                    .Select(date =>
                        date.Date)
                    .Where(date =>
                        companyWorkingDates.Contains(
                            date
                        ))
                    .ToHashSet();

            var approvedPaidLeaveDates =
                _context.PaidLeaveRequests
                    .AsNoTracking()
                    .Where(request =>
                        request.EmployeeId ==
                            employee.EmployeeId &&
                        request.Status ==
                            "承認" &&
                        request.Days >
                            0 &&
                        request.LeaveDate >=
                            checkStartDate &&
                        request.LeaveDate <
                            checkEndDateExclusive)
                    .Select(request =>
                        request.LeaveDate)
                    .ToList()
                    .Select(date =>
                        date.Date)
                    .Where(date =>
                        companyWorkingDates.Contains(
                            date
                        ))
                    .ToHashSet();

            // 同じ日に出勤打刻と有給が存在しても、
            // 出勤扱い日数は1日として数える
            actualAttendanceDates.UnionWith(
                approvedPaidLeaveDates
            );

            var attendedDays =
                actualAttendanceDates.Count;

            var ruleResult =
                _paidLeaveRuleService.Calculate(
                    employee.JoinDate,
                    grantPeriod.GrantDate,
                    totalWorkDays,
                    attendedDays,
                    0
                );

            if (ruleResult.IsEligible &&
                ruleResult.IsAttendanceRateEnough)
            {
                ruleResult.Message =
                    $"出勤率が80％以上のため、" +
                    $"年次有給休暇を" +
                    $"{ruleResult.GrantedDays}日付与しました。";
            }
            else if (totalWorkDays <= 0)
            {
                ruleResult.Message =
                    "出勤率確認期間の所定労働日が0日のため、" +
                    "有給を付与できませんでした。";
            }
            else
            {
                ruleResult.Message =
                    $"出勤率が" +
                    $"{ruleResult.AttendanceRate:0.#}％で" +
                    "80％未満のため、" +
                    "有給を付与できませんでした。";
            }

            return ruleResult;
        }

        /// <summary>
        /// 付与履歴ごとの使用日数と残日数を、
        /// 実際に取得済みの承認済み有給から再計算する。
        /// </summary>
        private void UpdateGrantHistoryUsage(
            int employeeId,
            DateTime targetDate)
        {
            targetDate =
                targetDate.Date;

            var histories =
                _context.PaidLeaveGrantHistories
                    .Where(history =>
                        history.EmployeeId ==
                            employeeId)
                    .OrderBy(history =>
                        history.GrantDate)
                    .ThenBy(history =>
                        history.PaidLeaveGrantHistoryId)
                    .ToList();

            if (!histories.Any())
            {
                return;
            }

            foreach (var history
                     in histories)
            {
                history.UsedDays =
                    0;

                history.RemainingDays =
                    history.GrantStatus == "付与"
                        ? Math.Max(
                            0,
                            history.GrantedDays
                        )
                        : 0;
            }

            var approvedRequests =
                _context.PaidLeaveRequests
                    .AsNoTracking()
                    .Where(request =>
                        request.EmployeeId ==
                            employeeId &&
                        request.Status ==
                            "承認" &&
                        request.Days >
                            0 &&
                        request.LeaveDate <=
                            targetDate)
                    .OrderBy(request =>
                        request.LeaveDate)
                    .ThenBy(request =>
                        request.CreatedAt)
                    .ThenBy(request =>
                        request.PaidLeaveRequestId)
                    .ToList();

            foreach (var request
                     in approvedRequests)
            {
                var leaveDate =
                    request.LeaveDate.Date;

                var remainingUsage =
                    Math.Max(
                        0,
                        request.Days
                    );

                var availableHistories =
                    histories
                        .Where(history =>
                            history.GrantStatus ==
                                "付与" &&
                            history.GrantDate.Date <=
                                leaveDate &&
                            (
                                history.ExpiryDate ??
                                history.GrantDate
                                    .AddYears(2)
                                    .AddDays(-1)
                            ).Date >=
                                leaveDate &&
                            history.RemainingDays >
                                0)
                        .OrderBy(history =>
                            (
                                history.ExpiryDate ??
                                history.GrantDate
                                    .AddYears(2)
                                    .AddDays(-1)
                            ))
                        .ThenBy(history =>
                            history.GrantDate)
                        .ToList();

                foreach (var history
                         in availableHistories)
                {
                    if (remainingUsage <= 0)
                    {
                        break;
                    }

                    var usedFromHistory =
                        Math.Min(
                            history.RemainingDays,
                            remainingUsage
                        );

                    history.UsedDays +=
                        usedFromHistory;

                    history.RemainingDays -=
                        usedFromHistory;

                    remainingUsage -=
                        usedFromHistory;
                }
            }

            var now =
                DateTime.Now;

            foreach (var history
                     in histories)
            {
                history.UsedDays =
                    Math.Max(
                        0,
                        history.UsedDays
                    );

                history.RemainingDays =
                    Math.Max(
                        0,
                        history.RemainingDays
                    );

                history.UpdatedAt =
                    now;
            }

            _context.SaveChanges();
        }

        private PaidLeaveRuleResult CalculatePaidLeaveRule(
            Employee employee)
        {
            var today = DateTime.Today;

            var firstResult = _paidLeaveRuleService.Calculate(
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

            // 同じ日は二重に数えない
            actualAttendanceDates.UnionWith(
                approvedPaidLeaveDates
            );

            var attendedDays =
                actualAttendanceDates.Count;

            var currentGrantDate =
                firstResult.CurrentGrantDate.Value;

            var usedDaysAfterCurrentGrant =
                _context.PaidLeaveRequests
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

        private void LoadNonWorkingDays(
            PaidLeaveCreateViewModel viewModel)
        {
            var startDate =
                DateTime.Today.Date;

            var endDate =
                DateTime.Today
                    .AddYears(2)
                    .Date;

            viewModel.NonWorkingDays =
                _context.CompanyCalendarDays
                    .AsNoTracking()
                    .Where(c =>
                        c.CalendarDate >= startDate &&
                        c.CalendarDate <= endDate &&
                        !c.IsWorkingDay)
                    .OrderBy(c =>
                        c.CalendarDate)
                    .Select(c =>
                        new PaidLeaveNonWorkingDayViewModel
                        {
                            CalendarDate =
                                c.CalendarDate,

                            DayType =
                                c.DayType,

                            HolidayName =
                                c.HolidayName ??
                                string.Empty
                        })
                    .ToList();
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
                        c.CalendarDate >= startDate &&
                        c.CalendarDate <= endDate)
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
                    isWorkingDay =
                        calendarWorkingDay;
                }
                else
                {
                    // カレンダー未作成期間の暫定処理
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
    }
}