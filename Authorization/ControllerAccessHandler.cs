using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Authorization
{
    public class ControllerAccessHandler : AuthorizationHandler<ControllerAccessRequirement>
    {
        private readonly ApplicationDbContext _context;
        private static readonly HashSet<string> _exemptControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Auth", "Home"
        };

        public ControllerAccessHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ControllerAccessRequirement requirement)
        {
            if (_exemptControllers.Contains(requirement.ControllerName))
            {
                context.Succeed(requirement);
                return;
            }

            if (context.User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var access = await _context.ControllerAccesses
                .Include(c => c.ControllerAccessRoles)
                    .ThenInclude(cr => cr.Role)
                .FirstOrDefaultAsync(c => c.ControllerName == requirement.ControllerName);

            if (access == null)
            {
                context.Succeed(requirement);
                return;
            }

            if (access.AllowAllAuthenticated)
            {
                context.Succeed(requirement);
                return;
            }

            if (!access.ControllerAccessRoles.Any())
            {
                context.Succeed(requirement);
                return;
            }

            var userRoleNames = context.User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var allowedRoleNames = access.ControllerAccessRoles
                .Select(cr => cr.Role.Name)
                .ToList();

            if (userRoleNames.Any(ur => allowedRoleNames.Contains(ur)))
            {
                context.Succeed(requirement);
            }
        }
    }
}
