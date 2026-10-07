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
    [Route("api/customers")]
    [Authorize]
    public class CustomersApiController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IUserScopeService _userScopeService;

        public CustomersApiController(ICustomerService customerService, IUserScopeService userScopeService)
        {
            _customerService = customerService;
            _userScopeService = userScopeService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CustomerDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var result = await _customerService.GetCustomersAsync(scope, search, status, null, "name", false, page, pageSize);

            var dtos = result.Items.Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                CreatedBy = c.CreatedBy,
                AssignedTo = c.AssignedTo,
                AssignedToName = c.AssignedUser?.FullName
            });

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetById(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var c = await _customerService.GetCustomerByIdAsync(id, scope);
            if (c == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found or access is denied." });
            }

            var dto = new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                CreatedBy = c.CreatedBy,
                AssignedTo = c.AssignedTo,
                AssignedToName = c.AssignedUser?.FullName
            };

            return Ok(dto);
        }

        [HttpPost]
        [ProducesResponseType(typeof(CustomerDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (!await _customerService.IsEmailUniqueAsync(dto.Email))
            {
                return Conflict(new { message = "A customer with this email address already exists." });
            }

            if (!await _customerService.IsPhoneUniqueAsync(dto.Phone))
            {
                return Conflict(new { message = "A customer with this phone number already exists." });
            }

            var customer = new Customer
            {
                CustomerName = dto.CustomerName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Address = dto.Address?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                Status = "Active",
                AssignedTo = scope.IsSalesExecutive ? scope.UserId : dto.AssignedTo
            };

            var (success, errorMessage, createdCustomer) = await _customerService.CreateCustomerAsync(customer, scope);
            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            var responseDto = new CustomerDto
            {
                CustomerId = createdCustomer!.CustomerId,
                CustomerCode = createdCustomer.CustomerCode,
                CustomerName = createdCustomer.CustomerName,
                Email = createdCustomer.Email,
                Phone = createdCustomer.Phone,
                CompanyName = createdCustomer.CompanyName,
                Address = createdCustomer.Address,
                City = createdCustomer.City,
                State = createdCustomer.State,
                Status = createdCustomer.Status,
                CreatedDate = createdCustomer.CreatedDate,
                CreatedBy = createdCustomer.CreatedBy,
                AssignedTo = createdCustomer.AssignedTo
            };

            return CreatedAtAction(nameof(GetById), new { id = responseDto.CustomerId }, responseDto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (!await _customerService.IsEmailUniqueAsync(dto.Email, id))
            {
                return Conflict(new { message = "Another customer with this email address already exists." });
            }

            if (!await _customerService.IsPhoneUniqueAsync(dto.Phone, id))
            {
                return Conflict(new { message = "Another customer with this phone number already exists." });
            }

            var customer = new Customer
            {
                CustomerId = id,
                CustomerName = dto.CustomerName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Address = dto.Address?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                Status = dto.Status,
                AssignedTo = dto.AssignedTo
            };

            var (success, errorMessage) = await _customerService.UpdateCustomerAsync(customer, scope);
            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = "Customer updated successfully." });
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var (success, errorMessage) = await _customerService.DeleteCustomerAsync(id, scope);
            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = "Customer deactivated successfully." });
        }
    }
}
