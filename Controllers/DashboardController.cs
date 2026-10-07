using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IUserScopeService _userScopeService;

        public DashboardController(IDashboardService dashboardService, IUserScopeService userScopeService)
        {
            _dashboardService = dashboardService;
            _userScopeService = userScopeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? dateFilter = "month", DateTime? startDate = null, DateTime? endDate = null)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var model = await _dashboardService.GetDashboardDataAsync(scope, dateFilter, startDate, endDate);
            return View(model);
        }
    }
}
