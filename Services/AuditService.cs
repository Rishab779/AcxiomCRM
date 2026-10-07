using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(string? userId, string? userName, string action, string entityName, string? recordId, string? oldValue = null, string? newValue = null, string? details = null);
        Task<PaginatedList<AuditLog>> GetAuditLogsAsync(UserScope? scope, string? search, string? entityFilter, string? actionFilter, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize);
    }

    public class PaginatedList<T>
    {
        public List<T> Items { get; }
        public int PageIndex { get; }
        public int TotalPages { get; }
        public int TotalCount { get; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalCount = count;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            Items = items;
        }
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string? userId, string? userName, string action, string entityName, string? recordId, string? oldValue = null, string? newValue = null, string? details = null)
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            // Sanitize values to ensure sensitive information is NEVER logged
            var sanitizedOld = Sanitize(oldValue);
            var sanitizedNew = Sanitize(newValue);

            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = sanitizedOld,
                NewValue = sanitizedNew,
                Details = details,
                IpAddress = ipAddress,
                CreatedDate = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<PaginatedList<AuditLog>> GetAuditLogsAsync(UserScope? scope, string? search, string? entityFilter, string? actionFilter, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (scope != null && !scope.IsAdmin)
            {
                if (scope.IsManager)
                {
                    var allowedUserIds = new List<string>(scope.ManagedUserIds) { scope.UserId };
                    query = query.Where(a => a.UserId != null && allowedUserIds.Contains(a.UserId));
                }
                else
                {
                    query = query.Where(a => a.UserId == scope.UserId);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a =>
                    (a.UserName != null && a.UserName.Contains(search)) ||
                    (a.Details != null && a.Details.Contains(search)) ||
                    (a.RecordId != null && a.RecordId.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(entityFilter))
            {
                query = query.Where(a => a.EntityName == entityFilter);
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action == actionFilter);
            }

            if (startDate.HasValue)
            {
                var utcStart = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(a => a.CreatedDate >= utcStart);
            }

            if (endDate.HasValue)
            {
                var utcEnd = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(a => a.CreatedDate <= utcEnd);
            }

            query = query.OrderByDescending(a => a.CreatedDate);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedList<AuditLog>(items, totalCount, pageIndex, pageSize);
        }

        private static string? Sanitize(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var lower = text.ToLowerInvariant();
            if (lower.Contains("password") || lower.Contains("token") || lower.Contains("hash") || lower.Contains("secret"))
            {
                return "[REDACTED_SECURITY_DATA]";
            }
            return text;
        }
    }
}
