using Microsoft.AspNetCore.Mvc.Filters;
using JwtAuthApp.Data;
using JwtAuthApp.Models;
using JwtAuthApp.Attributes;
using System.Security.Claims;
using System.Diagnostics;

namespace JwtAuthApp.Filters
{
    public class UserActionLogFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserActionLogFilter> _logger;

        public UserActionLogFilter(ApplicationDbContext context, ILogger<UserActionLogFilter> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (ShouldSkipLogging(context))
            {
                await next();
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var resultContext = await next();
            stopwatch.Stop();

            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
                return;

            try
            {
                var log = BuildActionLog(context, resultContext, stopwatch.ElapsedMilliseconds);
                _context.AuditLogs.Add(log);
                // Контроллер сам вызовет SaveChangesAsync() — лог сохранится вместе с бизнес-данными
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action log entry");
            }
        }

        private bool ShouldSkipLogging(ActionExecutingContext context)
        {
            var controllerHasSkip = context.Controller.GetType()
                .GetCustomAttributes(typeof(SkipLoggingAttribute), true)
                .Any();

            var actionHasSkip = context.ActionDescriptor.EndpointMetadata
                .Any(em => em.GetType() == typeof(SkipLoggingAttribute));

            if (controllerHasSkip || actionHasSkip)
                return true;

            var path = context.HttpContext.Request.Path.Value ?? "";
            return path.StartsWith("/css") || path.StartsWith("/js") ||
                   path.StartsWith("/lib") || path.StartsWith("/images");
        }

        private AuditLog BuildActionLog(ActionExecutingContext context, ActionExecutedContext resultContext, long elapsedMs)
        {
            var userIdClaim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out var userId);

            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
            var actionName = context.RouteData.Values["action"]?.ToString() ?? "Unknown";

            int.TryParse(context.RouteData.Values["id"]?.ToString(), out var targetId);

            return new AuditLog
            {
                Type = AuditLogType.Action,
                UserId = userId > 0 ? userId : null,
                UserName = context.HttpContext.User.Identity?.Name ?? "Unknown",
                Action = $"{controllerName}.{actionName}",
                Details = BuildActionDetails(context),
                TargetId = targetId > 0 ? targetId : null,
                HttpMethod = context.HttpContext.Request.Method,
                Url = context.HttpContext.Request.Path.Value ?? "",
                IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                Timestamp = DateTime.UtcNow,
                IsSuccess = resultContext.Exception == null || resultContext.ExceptionHandled,
                ExecutionTimeMs = elapsedMs
            };
        }

        private static string BuildActionDetails(ActionExecutingContext context)
        {
            var details = new List<string>();

            foreach (var param in context.ActionArguments)
            {
                if (param.Key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    param.Key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    param.Key.Contains("secret", StringComparison.OrdinalIgnoreCase))
                {
                    details.Add($"{param.Key}=[HIDDEN]");
                    continue;
                }

                if (param.Value != null && param.Value.GetType().IsClass && param.Value.GetType() != typeof(string))
                {
                    details.Add($"{param.Key}=[{param.Value.GetType().Name}]");
                }
                else if (param.Value != null)
                {
                    var value = param.Value.ToString();
                    if (value?.Length > 50)
                        value = value[..47] + "...";
                    details.Add($"{param.Key}={value}");
                }
                else
                {
                    details.Add($"{param.Key}=null");
                }
            }

            return details.Count > 0 ? string.Join(", ", details) : "No parameters";
        }
    }
}
