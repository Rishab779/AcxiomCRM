using System.Collections.Generic;
using System.Linq;

namespace AcxiomCRM.Services
{
    public class UserScope
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public IList<string> Roles { get; set; } = new List<string>();
        public List<string> ManagedUserIds { get; set; } = new List<string>();

        public bool IsAdmin => Roles.Contains("Admin");
        public bool IsManager => Roles.Contains("Manager");
        public bool IsSalesExecutive => Roles.Contains("Sales Executive");

        public bool CanAccess(string? assignedToUserId, string? createdByUserId = null)
        {
            if (IsAdmin) return true;

            if (IsManager)
            {
                if (string.IsNullOrEmpty(assignedToUserId)) return true;
                if (assignedToUserId == UserId) return true;
                if (ManagedUserIds.Contains(assignedToUserId)) return true;
                return false;
            }

            // Sales Executive
            if (assignedToUserId == UserId) return true;
            if (!string.IsNullOrEmpty(createdByUserId) && createdByUserId == UserId) return true;
            return false;
        }

        public bool IsAssignedOrManaged(string? assignedToUserId)
        {
            if (IsAdmin) return true;
            if (string.IsNullOrEmpty(assignedToUserId)) return IsAdmin || IsManager;
            if (assignedToUserId == UserId) return true;
            if (IsManager && ManagedUserIds.Contains(assignedToUserId)) return true;
            return false;
        }
    }
}
