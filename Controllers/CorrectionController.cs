using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using AttendanceManagementSystem.Services;
using AttendanceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagementSystem.Controllers
{
    public class CorrectionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OperationLogService _operationLogService;
        private readonly MonthlyClosingService _monthlyClosingService;

        public CorrectionController(
            ApplicationDbContext context,
            OperationLogService operationLogService,
            MonthlyClosingService monthlyClosingService)
        {
            _context = context;
            _operationLogService = operationLogService;
            _monthlyClosingService = monthlyClosingService;
        }

        [HttpGet]
        public IActionResult Create(
            DateTime? targetDate,
            string? correctionType)
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var employeeId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Employee" || employeeId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var date = (targetDate ?? DateTime.Today).Date;

            var type = correctionType == "退勤時間"
                ? "退勤時間"
                : "出勤時間";

            var attendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.EmployeeId == employeeId.Value &&
                    a.AttendanceDate == date);

            var beforeTime = GetBeforeTime(
                attendance,
                type);

            var viewModel = new CorrectionCreateViewModel
            {
                TargetDate = date,
                CorrectionType = type,
                BeforeTime = beforeTime,
                Message = TempData["Message"] as string
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
        CorrectionCreateViewModel viewModel)
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

            viewModel.TargetDate =
                viewModel.TargetDate.Date;

            // =====================================
            // 月次締め済みチェック
            // =====================================

            if (_monthlyClosingService.IsClosed(
                    viewModel.TargetDate))
            {
                _operationLogService.Write(
                    actionName:
                        "勤怠修正申請",

                    targetType:
                        "AttendanceCorrectionRequest",

                    details:
                        $"月次締め済みのため勤怠修正申請を拒否しました。" +
                        $"対象日：{viewModel.TargetDate:yyyy/MM/dd}、" +
                        $"修正項目：{viewModel.CorrectionType}",

                    result:
                        "失敗"
                );

                TempData["Message"] =
                    $"{viewModel.TargetDate:yyyy年MM月}は月次締め済みのため、" +
                    "勤怠修正申請できません。";

                return RedirectToAction(
                    "Create",
                    new
                    {
                        targetDate =
                            viewModel.TargetDate
                                .ToString(
                                    "yyyy-MM-dd"
                                ),

                        correctionType =
                            viewModel.CorrectionType
                    }
                );
            }

            // =====================================
            // 入力チェック
            // =====================================

            if (viewModel.CorrectionType !=
                    "出勤時間" &&
                viewModel.CorrectionType !=
                    "退勤時間")
            {
                ModelState.AddModelError(
                    "CorrectionType",
                    "修正項目を正しく選択してください。"
                );
            }

            if (!ModelState.IsValid)
            {
                _operationLogService.Write(
                    actionName:
                        "勤怠修正申請",

                    targetType:
                        "AttendanceCorrectionRequest",

                    details:
                        $"入力内容にエラーがあります。" +
                        $"対象日：{viewModel.TargetDate:yyyy/MM/dd}、" +
                        $"修正項目：{viewModel.CorrectionType}",

                    result:
                        "失敗"
                );

                return View(
                    viewModel
                );
            }

            var attendance =
                _context.Attendances
                    .FirstOrDefault(a =>
                        a.EmployeeId ==
                            employeeId.Value &&
                        a.AttendanceDate ==
                            viewModel.TargetDate
                    );

            // =====================================
            // 欠勤確定済みチェック
            // =====================================

            if (attendance?.IsAbsent == true)
            {
                _operationLogService.Write(
                    actionName:
                        "勤怠修正申請",

                    targetType:
                        "AttendanceCorrectionRequest",

                    targetId:
                        attendance.AttendanceId,

                    details:
                        $"欠勤確定済みのため勤怠修正申請を拒否しました。" +
                        $"対象日：{viewModel.TargetDate:yyyy/MM/dd}、" +
                        $"修正項目：{viewModel.CorrectionType}、" +
                        $"欠勤理由：{attendance.AbsenceReason ?? "未登録"}。",

                    result:
                        "失敗"
                );

                TempData["Message"] =
                    "対象日は管理者により欠勤確定されているため、" +
                    "勤怠修正申請できません。" +
                    "修正が必要な場合は、管理者に欠勤確定の取消を依頼してください。";

                return RedirectToAction(
                    "Create",
                    new
                    {
                        targetDate =
                            viewModel.TargetDate
                                .ToString(
                                    "yyyy-MM-dd"
                                ),

                        correctionType =
                            viewModel.CorrectionType
                    }
                );
            }

            // =====================================
            // 勤怠レコード作成
            // =====================================

            // 打刻データがない日でも修正申請できるよう、
            // 未出勤の勤怠データを作成する
            if (attendance == null)
            {
                attendance =
                    new Attendance
                    {
                        EmployeeId =
                            employeeId.Value,

                        AttendanceDate =
                            viewModel.TargetDate,

                        ClockInTime =
                            null,

                        ClockOutTime =
                            null,

                        BreakMinutes =
                            60,

                        WorkMinutes =
                            0,

                        LateMinutes =
                            0,

                        OvertimeMinutes =
                            0,

                        Status =
                            "未出勤",

                        CreatedAt =
                            DateTime.Now,

                        UpdatedAt =
                            DateTime.Now
                    };

                _context.Attendances.Add(
                    attendance
                );

                _context.SaveChanges();
            }

            // =====================================
            // 重複申請チェック
            // =====================================

            var duplicateRequest =
                _context.AttendanceCorrectionRequests
                    .Any(r =>
                        r.EmployeeId ==
                            employeeId.Value &&
                        r.AttendanceId ==
                            attendance.AttendanceId &&
                        r.TargetDate ==
                            viewModel.TargetDate &&
                        r.CorrectionType ==
                            viewModel.CorrectionType &&
                        r.Status ==
                            "申請中"
                    );

            if (duplicateRequest)
            {
                viewModel.BeforeTime =
                    GetBeforeTime(
                        attendance,
                        viewModel.CorrectionType
                    );

                ModelState.AddModelError(
                    "",
                    "同じ対象日・修正項目の申請がすでに申請中です。"
                );

                _operationLogService.Write(
                    actionName:
                        "勤怠修正申請",

                    targetType:
                        "AttendanceCorrectionRequest",

                    targetId:
                        attendance.AttendanceId,

                    details:
                        $"重複申請を拒否しました。" +
                        $"対象日：{viewModel.TargetDate:yyyy/MM/dd}、" +
                        $"修正項目：{viewModel.CorrectionType}",

                    result:
                        "失敗"
                );

                return View(
                    viewModel
                );
            }

            // =====================================
            // 修正申請保存
            // =====================================

            var beforeTime =
                GetBeforeTime(
                    attendance,
                    viewModel.CorrectionType
                );

            var request =
                new AttendanceCorrectionRequest
                {
                    EmployeeId =
                        employeeId.Value,

                    AttendanceId =
                        attendance.AttendanceId,

                    TargetDate =
                        viewModel.TargetDate,

                    CorrectionType =
                        viewModel.CorrectionType,

                    BeforeTime =
                        beforeTime,

                    AfterTime =
                        viewModel.AfterTime,

                    Reason =
                        viewModel.Reason,

                    Status =
                        "申請中",

                    CreatedAt =
                        DateTime.Now
                };

            _context.AttendanceCorrectionRequests
                .Add(
                    request
                );

            _context.SaveChanges();

            var beforeTimeText =
                beforeTime.HasValue
                    ? beforeTime.Value.ToString(
                        @"hh\:mm"
                    )
                    : "-";

            var afterTimeText =
                viewModel.AfterTime.ToString(
                    @"hh\:mm"
                );

            _operationLogService.Write(
                actionName:
                    "勤怠修正申請",

                targetType:
                    "AttendanceCorrectionRequest",

                targetId:
                    request.RequestId,

                details:
                    $"勤怠修正申請を送信しました。" +
                    $"対象日：{viewModel.TargetDate:yyyy/MM/dd}、" +
                    $"修正項目：{viewModel.CorrectionType}、" +
                    $"修正前：{beforeTimeText}、" +
                    $"修正後：{afterTimeText}",

                result:
                    "成功"
            );

            TempData["Message"] =
                "勤怠修正申請を送信しました。";

            return RedirectToAction(
                "Create"
            );
        }

        [HttpGet]
        public IActionResult History()
        {
            var role =
                HttpContext.Session.GetString("LoginUserRole");

            var employeeId =
                HttpContext.Session.GetInt32("LoginUserId");

            if (role != "Employee" || employeeId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var requests = _context.AttendanceCorrectionRequests
                .Where(r => r.EmployeeId == employeeId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var approverIds = requests
                .Where(r => r.ApprovedBy.HasValue)
                .Select(r => r.ApprovedBy!.Value)
                .Distinct()
                .ToList();

            var approverNames = _context.Employees
                .Where(e => approverIds.Contains(e.EmployeeId))
                .ToDictionary(
                    e => e.EmployeeId,
                    e => e.Name
                );

            var viewModel = new CorrectionHistoryViewModel
            {
                Items = requests
                    .Select(r => new CorrectionHistoryItemViewModel
                    {
                        RequestId = r.RequestId,
                        TargetDate = r.TargetDate,
                        CorrectionType = r.CorrectionType,
                        BeforeTime = r.BeforeTime,
                        AfterTime = r.AfterTime,
                        Reason = r.Reason,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        ApprovedAt = r.ApprovedAt,
                        ApprovedBy = r.ApprovedBy,
                        AdminComment = r.AdminComment ?? "",
                        ApprovedByName =
                            r.ApprovedBy.HasValue &&
                            approverNames.TryGetValue(
                                r.ApprovedBy.Value,
                                out var approverName)
                                    ? approverName
                                    : "-"
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        private TimeSpan? GetBeforeTime(
            Attendance? attendance,
            string correctionType)
        {
            if (attendance == null)
            {
                return null;
            }

            if (correctionType == "出勤時間")
            {
                return attendance.ClockInTime;
            }

            if (correctionType == "退勤時間")
            {
                return attendance.ClockOutTime;
            }

            return null;
        }
    }
}