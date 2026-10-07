using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Api
{
    [ApiController]
    [Route("api/leads")]
    [Authorize]
    public class LeadsApiController : ControllerBase
    {
        private readonly ILeadService _leadService;
        private readonly IUserScopeService _userScopeService;

        public LeadsApiController(ILeadService leadService, IUserScopeService userScopeService)
        {
            _leadService = leadService;
            _userScopeService = userScopeService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LeadDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var result = await _leadService.GetLeadsAsync(scope, search, status, null, null, "name", false, page, pageSize);

            var dtos = result.Items.Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedTo = l.AssignedTo,
                AssignedToName = l.AssignedUser?.FullName,
                ConvertedCustomerId = l.ConvertedCustomerId
            });

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LeadDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var l = await _leadService.GetLeadByIdAsync(id, scope);
            if (l == null)
            {
                return NotFound(new { message = $"Lead with ID {id} was not found or access is denied." });
            }

            var dto = new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedTo = l.AssignedTo,
                AssignedToName = l.AssignedUser?.FullName,
                ConvertedCustomerId = l.ConvertedCustomerId
            };

            return Ok(dto);
        }

        [HttpPost]
        [ProducesResponseType(typeof(LeadDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateLeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var scope = await _userScopeService.GetUserScopeAsync(User);

            var lead = new Lead
            {
                LeadName = dto.LeadName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Source = dto.Source,
                Priority = dto.Priority,
                ExpectedValue = dto.ExpectedValue,
                AssignedTo = scope.IsSalesExecutive ? scope.UserId : dto.AssignedTo
            };

            var (success, errorMessage, createdLead) = await _leadService.CreateLeadAsync(lead, scope);
            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            var responseDto = new LeadDto
            {
                LeadId = createdLead!.LeadId,
                LeadCode = createdLead.LeadCode,
                LeadName = createdLead.LeadName,
                Email = createdLead.Email,
                Phone = createdLead.Phone,
                CompanyName = createdLead.CompanyName,
                Source = createdLead.Source,
                Status = createdLead.Status,
                Priority = createdLead.Priority,
                ExpectedValue = createdLead.ExpectedValue,
                CreatedDate = createdLead.CreatedDate,
                AssignedTo = createdLead.AssignedTo
            };

            return CreatedAtAction(nameof(GetById), new { id = responseDto.LeadId }, responseDto);
        }
    }
}
