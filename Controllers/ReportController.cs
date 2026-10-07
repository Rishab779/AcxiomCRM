using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IUserScopeService _userScopeService;

        public ReportController(IReportService reportService, IUserScopeService userScopeService)
        {
            _reportService = reportService;
            _userScopeService = userScopeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            ViewBag.IsAdmin = scope.IsAdmin;
            ViewBag.IsManager = scope.IsManager;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CustomerReport(string? status, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var report = await _reportService.GetCustomerReportAsync(scope, status, startDate, endDate, page, 20);

            ViewBag.Status = status;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> LeadReport(string? status, string? source, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var report = await _reportService.GetLeadReportAsync(scope, status, source, startDate, endDate, page, 20);

            ViewBag.Status = status;
            ViewBag.Source = source;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> FollowUpReport(string? status, string? type, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var report = await _reportService.GetFollowUpReportAsync(scope, status, type, startDate, endDate, page, 20);

            ViewBag.Status = status;
            ViewBag.Type = type;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> OpportunityReport(string? stage, string? status, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var report = await _reportService.GetOpportunityReportAsync(scope, stage, status, startDate, endDate, page, 20);

            ViewBag.Stage = stage;
            ViewBag.Status = status;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> PipelineReport()
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var (stages, owners, totalWeighted) = await _reportService.GetPipelineReportAsync(scope);

            ViewBag.Stages = stages;
            ViewBag.Owners = owners;
            ViewBag.TotalWeighted = totalWeighted;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConversionReport(DateTime? startDate, DateTime? endDate)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var report = await _reportService.GetConversionReportAsync(scope, startDate, endDate);

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            return View(report);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UserActivityReport()
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var report = await _reportService.GetUserActivityReportAsync(scope);
            return View(report);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AuditReport(string? entityName, string? action, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var report = await _reportService.GetAuditReportAsync(scope, entityName, action, startDate, endDate, page, 25);

            ViewBag.EntityName = entityName;
            ViewBag.Action = action;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            return View(report);
        }
    }
}
