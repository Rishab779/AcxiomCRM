using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AuditController : Controller
    {
        private readonly IAuditService _auditService;
        private readonly IUserScopeService _userScopeService;

        public AuditController(IAuditService auditService, IUserScopeService userScopeService)
        {
            _auditService = auditService;
            _userScopeService = userScopeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? entityFilter, string? actionFilter, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var logs = await _auditService.GetAuditLogsAsync(scope, search, entityFilter, actionFilter, startDate, endDate, page, 25);

            ViewBag.Search = search;
            ViewBag.EntityFilter = entityFilter;
            ViewBag.ActionFilter = actionFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(logs);
        }
    }
}
