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
    [Route("api/opportunities")]
    [Authorize]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly IOpportunityService _opportunityService;
        private readonly IUserScopeService _userScopeService;

        public OpportunitiesApiController(IOpportunityService opportunityService, IUserScopeService userScopeService)
        {
            _opportunityService = opportunityService;
            _userScopeService = userScopeService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OpportunityDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? stage, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var result = await _opportunityService.GetOpportunitiesAsync(scope, search, stage, status, null, "amount", true, page, pageSize);

            var dtos = result.Items.Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer.CustomerName,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                WeightedAmount = o.WeightedAmount,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                AssignedTo = o.AssignedTo,
                AssignedToName = o.AssignedUser?.FullName
            });

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OpportunityDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var o = await _opportunityService.GetOpportunityByIdAsync(id, scope);
            if (o == null)
            {
                return NotFound(new { message = $"Opportunity with ID {id} was not found or access is denied." });
            }

            var dto = new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer.CustomerName,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                WeightedAmount = o.WeightedAmount,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                AssignedTo = o.AssignedTo,
                AssignedToName = o.AssignedUser?.FullName
            };

            return Ok(dto);
        }

        [HttpPost]
        [ProducesResponseType(typeof(OpportunityDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateOpportunityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var scope = await _userScopeService.GetUserScopeAsync(User);

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName.Trim(),
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Notes = dto.Notes,
                AssignedTo = scope.IsSalesExecutive ? scope.UserId : dto.AssignedTo
            };

            var (success, errorMessage, createdOpp) = await _opportunityService.CreateOpportunityAsync(opp, scope);
            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            var responseDto = new OpportunityDto
            {
                OpportunityId = createdOpp!.OpportunityId,
                OpportunityName = createdOpp.OpportunityName,
                CustomerId = createdOpp.CustomerId,
                CustomerName = createdOpp.Customer?.CustomerName ?? string.Empty,
                LeadId = createdOpp.LeadId,
                Amount = createdOpp.Amount,
                Stage = createdOpp.Stage,
                Probability = createdOpp.Probability,
                WeightedAmount = createdOpp.WeightedAmount,
                ExpectedCloseDate = createdOpp.ExpectedCloseDate,
                Status = createdOpp.Status,
                CreatedDate = createdOpp.CreatedDate,
                AssignedTo = createdOpp.AssignedTo
            };

            return CreatedAtAction(nameof(GetById), new { id = responseDto.OpportunityId }, responseDto);
        }
    }
}
