using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Helpers;
using AttendanceManagementSystem.Services;
using AttendanceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OperationLogService _operationLogService;

        private const int MaxFailedLoginAttempts = 5;
        private const int LockoutMinutes = 15;

        public AccountController(
            ApplicationDbContext context,
            OperationLogService operationLogService)
        {
            _context = context;
            _operationLogService = operationLogService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        public IActionResult Login(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _operationLogService.Write(
                    actionName: "ログイン失敗",
                    targetType: "Account",
                    details: "メールアドレスが未入力です。",
                    result: "失敗"
                );

                ViewBag.ErrorMessage =
                    "メールアドレスを入力してください。";

                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                _operationLogService.Write(
                    actionName: "ログイン失敗",
                    targetType: "Account",
                    details:
                        $"パスワードが未入力です。" +
                        $"入力メール：{email.Trim()}",
                    result: "失敗"
                );

                ViewBag.ErrorMessage =
                    "パスワードを入力してください。";

                return View();
            }

            var normalizedEmail = email.Trim();
            var now = DateTime.Now;

            var employee = _context.Employees
                .FirstOrDefault(e =>
                    e.Email == normalizedEmail);

            // 登録されていないメールアドレス
            if (employee == null)
            {
                _operationLogService.Write(
                    actionName: "ログイン失敗",
                    targetType: "Account",
                    details:
                        $"登録されていないメールアドレスで" +
                        $"ログインが試行されました。" +
                        $"入力メール：{normalizedEmail}",
                    result: "失敗",
                    userName: "Unknown",
                    role: "Unknown"
                );

                ViewBag.ErrorMessage =
                    "メールアドレスまたはパスワードが" +
                    "正しくありません。";

                return View();
            }

            // 現在ロック中
            if (employee.LockoutEndAt.HasValue &&
                employee.LockoutEndAt.Value > now)
            {
                var remainingMinutes = Math.Max(
                    1,
                    (int)Math.Ceiling(
                        (employee.LockoutEndAt.Value - now)
                            .TotalMinutes
                    )
                );

                _operationLogService.Write(
                    actionName: "ログイン拒否",
                    targetType: "Employee",
                    targetId: employee.EmployeeId,
                    details:
                        $"アカウントロック中のため" +
                        $"ログインを拒否しました。" +
                        $"残り約{remainingMinutes}分。",
                    result: "ロック中",
                    employeeId: employee.EmployeeId,
                    userName: employee.Name,
                    role: employee.Role
                );

                ViewBag.ErrorMessage =
                    "ログインに複数回失敗したため、" +
                    "アカウントは一時的にロックされています。" +
                    $"約{remainingMinutes}分後に" +
                    "再度お試しください。";

                return View();
            }

            // ロック時間終了後に状態をリセット
            if (employee.LockoutEndAt.HasValue &&
                employee.LockoutEndAt.Value <= now)
            {
                employee.FailedLoginCount = 0;
                employee.LastFailedLoginAt = null;
                employee.LockoutEndAt = null;
                employee.UpdatedAt = now;

                _context.SaveChanges();
            }

            var passwordIsCorrect =
                PasswordHelper.VerifyPassword(
                    password,
                    employee.PasswordHash
                );

            // パスワード不一致
            if (!passwordIsCorrect)
            {
                employee.FailedLoginCount++;
                employee.LastFailedLoginAt = now;
                employee.UpdatedAt = now;

                if (employee.FailedLoginCount >=
                    MaxFailedLoginAttempts)
                {
                    employee.LockoutEndAt =
                        now.AddMinutes(LockoutMinutes);

                    _context.SaveChanges();

                    _operationLogService.Write(
                        actionName: "ログイン失敗",
                        targetType: "Employee",
                        targetId: employee.EmployeeId,
                        details:
                            $"ログインに" +
                            $"{MaxFailedLoginAttempts}回" +
                            $"失敗しました。",
                        result: "失敗",
                        employeeId: employee.EmployeeId,
                        userName: employee.Name,
                        role: employee.Role
                    );

                    _operationLogService.Write(
                        actionName: "アカウントロック",
                        targetType: "Employee",
                        targetId: employee.EmployeeId,
                        details:
                            $"{LockoutMinutes}分間" +
                            $"アカウントをロックしました。" +
                            $"解除予定：" +
                            $"{employee.LockoutEndAt:yyyy/MM/dd HH:mm}",
                        result: "実行",
                        employeeId: employee.EmployeeId,
                        userName: employee.Name,
                        role: employee.Role
                    );

                    ViewBag.ErrorMessage =
                        $"ログインに" +
                        $"{MaxFailedLoginAttempts}回" +
                        $"失敗したため、" +
                        $"{LockoutMinutes}分間" +
                        $"アカウントをロックしました。";

                    return View();
                }

                _context.SaveChanges();

                var remainingAttempts =
                    MaxFailedLoginAttempts -
                    employee.FailedLoginCount;

                _operationLogService.Write(
                    actionName: "ログイン失敗",
                    targetType: "Employee",
                    targetId: employee.EmployeeId,
                    details:
                        $"パスワードが正しくありません。" +
                        $"失敗回数：" +
                        $"{employee.FailedLoginCount}回。",
                    result: "失敗",
                    employeeId: employee.EmployeeId,
                    userName: employee.Name,
                    role: employee.Role
                );

                ViewBag.ErrorMessage =
                    "メールアドレスまたはパスワードが" +
                    "正しくありません。" +
                    $"あと{remainingAttempts}回失敗すると" +
                    "アカウントがロックされます。";

                return View();
            }

            // 無効ユーザー
            if (!employee.IsActive)
            {
                employee.FailedLoginCount = 0;
                employee.LastFailedLoginAt = null;
                employee.LockoutEndAt = null;
                employee.UpdatedAt = now;

                _context.SaveChanges();

                _operationLogService.Write(
                    actionName: "ログイン拒否",
                    targetType: "Employee",
                    targetId: employee.EmployeeId,
                    details:
                        "無効化されているユーザーの" +
                        "ログインを拒否しました。",
                    result: "無効",
                    employeeId: employee.EmployeeId,
                    userName: employee.Name,
                    role: employee.Role
                );

                ViewBag.ErrorMessage =
                    "このユーザーは無効です。" +
                    "管理者にお問い合わせください。";

                return View();
            }

            // ログイン成功
            employee.FailedLoginCount = 0;
            employee.LastFailedLoginAt = null;
            employee.LockoutEndAt = null;
            employee.UpdatedAt = now;

            _context.SaveChanges();

            HttpContext.Session.SetInt32(
                "LoginUserId",
                employee.EmployeeId
            );

            HttpContext.Session.SetString(
                "LoginUserName",
                employee.Name
            );

            HttpContext.Session.SetString(
                "LoginUserRole",
                employee.Role
            );

            _operationLogService.Write(
                actionName: "ログイン",
                targetType: "Employee",
                targetId: employee.EmployeeId,
                details: "ログインに成功しました。",
                result: "成功",
                employeeId: employee.EmployeeId,
                userName: employee.Name,
                role: employee.Role
            );

            // 管理者が発行した初期パスワードで初回ログインした場合、
            // パスワード変更画面へ自動的に移動する
            if (employee.MustChangePassword)
            {
                TempData["PasswordChangeMessage"] =
                    "初回ログインのため、パスワードを変更してください。";

                return RedirectToAction(
                    nameof(ChangePassword)
                );
            }

            if (employee.Role == "Admin")
            {
                return RedirectToAction(
                    "Index",
                    "Admin"
                );
            }

            return RedirectToAction(
                "Index",
                "Attendance"
            );
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            var employeeId =
                HttpContext.Session.GetInt32("LoginUserId");

            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (employeeId == null ||
                (role != "Employee" && role != "Admin"))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var employeeExists = _context.Employees
                .Any(e =>
                    e.EmployeeId == employeeId.Value &&
                    e.IsActive);

            if (!employeeExists)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            return View(
                new ChangePasswordViewModel()
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(
            ChangePasswordViewModel viewModel)
        {
            var employeeId =
                HttpContext.Session.GetInt32("LoginUserId");

            var role =
                HttpContext.Session.GetString("LoginUserRole");

            if (employeeId == null ||
                (role != "Employee" && role != "Admin"))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var employee = _context.Employees
                .FirstOrDefault(e =>
                    e.EmployeeId == employeeId.Value &&
                    e.IsActive);

            if (employee == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!ModelState.IsValid)
            {
                _operationLogService.Write(
                    actionName: "パスワード変更",
                    targetType: "Employee",
                    targetId: employee.EmployeeId,
                    details:
                        "入力内容にエラーがあるため、" +
                        "パスワード変更を中止しました。",
                    result: "失敗",
                    employeeId: employee.EmployeeId,
                    userName: employee.Name,
                    role: employee.Role
                );

                return View(viewModel);
            }

            var currentPasswordIsCorrect =
                PasswordHelper.VerifyPassword(
                    viewModel.CurrentPassword,
                    employee.PasswordHash
                );

            if (!currentPasswordIsCorrect)
            {
                ModelState.AddModelError(
                    nameof(viewModel.CurrentPassword),
                    "現在のパスワードが正しくありません。"
                );

                _operationLogService.Write(
                    actionName: "パスワード変更",
                    targetType: "Employee",
                    targetId: employee.EmployeeId,
                    details:
                        "現在のパスワードが一致しないため、" +
                        "パスワード変更を拒否しました。",
                    result: "失敗",
                    employeeId: employee.EmployeeId,
                    userName: employee.Name,
                    role: employee.Role
                );

                return View(viewModel);
            }

            var isSameAsCurrentPassword =
                PasswordHelper.VerifyPassword(
                    viewModel.NewPassword,
                    employee.PasswordHash
                );

            if (isSameAsCurrentPassword)
            {
                ModelState.AddModelError(
                    nameof(viewModel.NewPassword),
                    "現在のパスワードと異なる" +
                    "パスワードを設定してください。"
                );

                _operationLogService.Write(
                    actionName: "パスワード変更",
                    targetType: "Employee",
                    targetId: employee.EmployeeId,
                    details:
                        "現在と同じパスワードが指定されたため、" +
                        "パスワード変更を拒否しました。",
                    result: "失敗",
                    employeeId: employee.EmployeeId,
                    userName: employee.Name,
                    role: employee.Role
                );

                return View(viewModel);
            }

            var wasInitialPasswordChange =
                employee.MustChangePassword;

            employee.PasswordHash =
                PasswordHelper.HashPassword(
                    viewModel.NewPassword
                );

            // 初回パスワード変更が完了したため、
            // 次回以降は通常ログインにする
            employee.MustChangePassword = false;
            employee.FailedLoginCount = 0;
            employee.LastFailedLoginAt = null;
            employee.LockoutEndAt = null;
            employee.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            _operationLogService.Write(
                actionName: "パスワード変更",
                targetType: "Employee",
                targetId: employee.EmployeeId,
                details:
                    wasInitialPasswordChange
                        ? "初回ログイン時のパスワード変更が完了しました。"
                        : "本人操作によりパスワードを変更しました。",
                result: "成功",
                employeeId: employee.EmployeeId,
                userName: employee.Name,
                role: employee.Role
            );

            TempData["SuccessMessage"] =
                wasInitialPasswordChange
                    ? "初回パスワードの設定が完了しました。"
                    : "パスワードを変更しました。";

            return RedirectToAction(
                nameof(ChangePassword)
            );
        }

        public IActionResult Logout()
        {
            var employeeId =
                HttpContext.Session.GetInt32("LoginUserId");

            var userName =
                HttpContext.Session.GetString("LoginUserName")
                ?? "Unknown";

            var role =
                HttpContext.Session.GetString("LoginUserRole")
                ?? "Unknown";

            _operationLogService.Write(
                actionName: "ログアウト",
                targetType: "Employee",
                targetId: employeeId,
                details: "ログアウトしました。",
                result: "成功",
                employeeId: employeeId,
                userName: userName,
                role: role
            );

            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account"
            );
        }
    }
}