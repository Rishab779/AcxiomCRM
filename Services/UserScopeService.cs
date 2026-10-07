using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface IUserScopeService
    {
        Task<UserScope> GetUserScopeAsync(ClaimsPrincipal principal);
        Task<List<ApplicationUser>> GetAssignableUsersAsync(UserScope scope);
    }

    public class UserScopeService : IUserScopeService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserScopeService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<UserScope> GetUserScopeAsync(ClaimsPrincipal principal)
        {
            var userId = _userManager.GetUserId(principal);
            if (string.IsNullOrEmpty(userId))
            {
                return new UserScope();
            }

            var user = await _userManager.FindByIdAsync(userId);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

            var managedUserIds = new List<string>();
            if (roles.Contains("Manager"))
            {
                managedUserIds = await _context.Users
                    .Where(u => u.ManagerId == userId)
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            return new UserScope
            {
                UserId = userId,
                UserName = user?.UserName ?? principal.Identity?.Name ?? string.Empty,
                Roles = roles,
                ManagedUserIds = managedUserIds
            };
        }

        public async Task<List<ApplicationUser>> GetAssignableUsersAsync(UserScope scope)
        {
            var query = _context.Users.Where(u => u.IsActive);
            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                {
                    query = query.Where(u => u.Id == scope.UserId || scope.ManagedUserIds.Contains(u.Id));
                }
                else
                {
                    query = query.Where(u => u.Id == scope.UserId);
                }
            }
            return await query.OrderBy(u => u.FullName).ToListAsync();
        }
    }
}
