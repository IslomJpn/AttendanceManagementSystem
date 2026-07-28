using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AttendanceManagementSystem.Services
{
    public class OperationLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<OperationLogService> _logger;

        public OperationLogService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<OperationLogService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public void Write(
            string actionName,
            string targetType = "",
            int? targetId = null,
            string details = "",
            string result = "成功",
            int? employeeId = null,
            string? userName = null,
            string? role = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                employeeId ??=
                    httpContext?.Session.GetInt32("LoginUserId");

                userName ??=
                    httpContext?.Session.GetString("LoginUserName")
                    ?? "Unknown";

                role ??=
                    httpContext?.Session.GetString("LoginUserRole")
                    ?? "Unknown";

                var userAgent =
                    httpContext?.Request.Headers.UserAgent.ToString()
                    ?? string.Empty;

                var operationLog = new OperationLog
                {
                    EmployeeId = employeeId,
                    UserName = LimitLength(userName, 100),
                    Role = LimitLength(role, 20),
                    ActionName = LimitLength(actionName, 100),
                    TargetType = LimitLength(targetType, 100),
                    TargetId = targetId,
                    Details = LimitLength(details, 1000),
                    Result = LimitLength(result, 20),
                    IpAddress = LimitLength(GetIpAddress(httpContext), 100),
                    UserAgent = LimitLength(userAgent, 500),
                    DeviceType = DetectDeviceType(userAgent),
                    CreatedAt = DateTime.Now
                };

                _context.OperationLogs.Add(operationLog);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                // ログ保存失敗によって本来の業務処理を停止させない
                _logger.LogWarning(
                    ex,
                    "操作ログの保存に失敗しました。ActionName: {ActionName}",
                    actionName);
            }
        }

        private string GetIpAddress(HttpContext? httpContext)
        {
            if (httpContext == null)
            {
                return string.Empty;
            }

            var forwardedFor =
                httpContext.Request.Headers["X-Forwarded-For"]
                    .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor
                    .Split(',')
                    .First()
                    .Trim();
            }

            return httpContext.Connection.RemoteIpAddress?.ToString()
                ?? string.Empty;
        }

        private string DetectDeviceType(string userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return "Unknown";
            }

            var value = userAgent.ToLowerInvariant();

            if (value.Contains("ipad") ||
                value.Contains("tablet"))
            {
                return "Tablet";
            }

            if (value.Contains("iphone") ||
                value.Contains("android") ||
                value.Contains("mobile"))
            {
                return "Mobile";
            }

            return "PC";
        }

        private string LimitLength(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= maxLength
                ? value
                : value[..maxLength];
        }
    }
}