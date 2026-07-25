using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;

namespace JwtAuthApp.Middleware
{
    public class ControllerAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly HashSet<string> _exemptControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Auth", "Home"
        };

        public ControllerAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var controllerName = context.GetRouteValue("controller")?.ToString();

            if (string.IsNullOrEmpty(controllerName) || _exemptControllers.Contains(controllerName))
            {
                await _next(context);
                return;
            }

            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var access = await db.ControllerAccesses
                .Include(c => c.ControllerAccessRoles)
                    .ThenInclude(cr => cr.Role)
                .FirstOrDefaultAsync(c => c.ControllerName == controllerName);

            if (access == null)
            {
                await DenyAccess(context, controllerName);
                return;
            }

            if (access.AllowAllAuthenticated)
            {
                await _next(context);
                return;
            }

            if (!access.ControllerAccessRoles.Any())
            {
                await _next(context);
                return;
            }

            var userRoleNames = context.User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var allowedRoleNames = access.ControllerAccessRoles
                .Select(cr => cr.Role.Name)
                .ToList();

            if (!userRoleNames.Any(ur => allowedRoleNames.Contains(ur)))
            {
                await DenyAccess(context, controllerName);
                return;
            }

            await _next(context);
        }

        private static async Task DenyAccess(HttpContext context, string controllerName)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            if (context.Request.Headers.ContainsKey("Accept") &&
                context.Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Forbidden", controller = controllerName });
            }
            else
            {
                context.Response.Redirect("/Home/Index");
            }
        }
    }
}
