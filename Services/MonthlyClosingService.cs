using AttendanceManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagementSystem.Services
{
    public class MonthlyClosingService
    {
        private readonly ApplicationDbContext _context;

        public MonthlyClosingService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public bool IsClosed(DateTime targetDate)
        {
            return IsClosed(
                targetDate.Year,
                targetDate.Month
            );
        }

        public bool IsClosed(
            int targetYear,
            int targetMonth)
        {
            if (targetYear < 2000 ||
                targetYear > 2100 ||
                targetMonth < 1 ||
                targetMonth > 12)
            {
                return false;
            }

            return _context.MonthlyClosings
                .AsNoTracking()
                .Any(m =>
                    m.TargetYear == targetYear &&
                    m.TargetMonth == targetMonth &&
                    m.IsClosed);
        }
    }
}