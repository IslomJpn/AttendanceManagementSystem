using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;

namespace AttendanceManagementSystem.Services
{
    public class AttendanceStampLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AttendanceStampLogService> _logger;

        public AttendanceStampLogService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AttendanceStampLogService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public void Write(
            int attendanceId,
            int employeeId,
            string stampType,
            decimal? latitude = null,
            decimal? longitude = null,
            double? accuracyMeters = null,
            string gpsStatus = "未取得",
            string result = "成功",
            string details = "")
        {
            try
            {
                var httpContext =
                    _httpContextAccessor.HttpContext;

                var ipAddress =
                    httpContext?.Connection.RemoteIpAddress?
                        .ToString() ?? "";

                var userAgent =
                    httpContext?.Request.Headers.UserAgent
                        .ToString() ?? "";

                var deviceType =
                    DetectDeviceType(userAgent);

                var log = new AttendanceStampLog
                {
                    AttendanceId = attendanceId,
                    EmployeeId = employeeId,
                    StampType = stampType,
                    StampedAt = DateTime.Now,
                    Latitude = latitude,
                    Longitude = longitude,
                    AccuracyMeters = accuracyMeters,
                    GpsStatus = gpsStatus,
                    DeviceType = deviceType,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Result = result,
                    Details = details,
                    CreatedAt = DateTime.Now
                };

                _context.AttendanceStampLogs.Add(log);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                // 打刻本体を停止させないため、
                // ログ保存エラーは記録のみ行う
                _logger.LogError(
                    ex,
                    "打刻ログの保存に失敗しました。");
            }
        }

        private string DetectDeviceType(string userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return "Unknown";
            }

            var lowerUserAgent =
                userAgent.ToLowerInvariant();

            if (lowerUserAgent.Contains("ipad") ||
                lowerUserAgent.Contains("tablet"))
            {
                return "Tablet";
            }

            if (lowerUserAgent.Contains("iphone") ||
                lowerUserAgent.Contains("android") ||
                lowerUserAgent.Contains("mobile"))
            {
                return "Mobile";
            }

            return "PC";
        }
    }
}