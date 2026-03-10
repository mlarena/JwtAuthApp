using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;
using JwtAuthApp.Attributes;
using System.Security.Claims;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthApp.Filters
{
    /// <summary>
    /// Фильтр для глобального логирования действий пользователей
    /// </summary>
    public class UserActionLogFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserActionLogFilter> _logger;
        private readonly Stopwatch _stopwatch;

        public UserActionLogFilter(ApplicationDbContext context, ILogger<UserActionLogFilter> logger)
        {
            _context = context;
            _logger = logger;
            _stopwatch = new Stopwatch();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _stopwatch.Start();

            // Проверяем, нужно ли пропустить логирование
            if (ShouldSkipLogging(context))
            {
                await next();
                return;
            }

            // Выполняем действие
            var resultContext = await next();
            _stopwatch.Stop();

            // Логируем только для авторизованных пользователей
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    await LogUserAction(context, resultContext);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при логировании действия пользователя");
                }
            }
        }

        private bool ShouldSkipLogging(ActionExecutingContext context)
        {
            // Проверяем наличие атрибута SkipLogging на контроллере
            var controllerHasSkip = context.Controller.GetType()
                .GetCustomAttributes(typeof(SkipLoggingAttribute), true)
                .Any();

            // Проверяем наличие атрибута SkipLogging на действии
            var actionHasSkip = context.ActionDescriptor.EndpointMetadata
                .Any(em => em.GetType() == typeof(SkipLoggingAttribute));

            // Пропускаем статические файлы
            var path = context.HttpContext.Request.Path.Value ?? "";
            if (path.StartsWith("/css") || path.StartsWith("/js") || 
                path.StartsWith("/lib") || path.StartsWith("/images"))
            {
                return true;
            }

            return controllerHasSkip || actionHasSkip;
        }

        private async Task LogUserAction(ActionExecutingContext context, ActionExecutedContext resultContext)
        {
            // Получаем информацию о пользователе
            var userIdClaim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userId = null;
            if (userIdClaim != null && int.TryParse(userIdClaim, out var parsed))
                userId = parsed;
            var userName = context.HttpContext.User.Identity?.Name ?? "Unknown";
            
            // Получаем название контроллера и действия
            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
            var actionName = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
            
            // Формируем детальную информацию
            var details = BuildActionDetails(context);
            
            // Получаем ID из маршрута
            int? targetId = null;
            if (context.RouteData.Values["id"] != null)
            {
                int.TryParse(context.RouteData.Values["id"]?.ToString(), out var id);
                targetId = id;
            }

            // Определяем успешность выполнения
            var isSuccess = resultContext.Exception == null || resultContext.ExceptionHandled;

            // Создаем запись лога
            var log = new AuditLog
            {
                Type = AuditLogType.Action,
                UserId = userId,
                UserName = userName,
                Action = $"{controllerName}.{actionName}",
                Details = details,
                TargetId = targetId,
                HttpMethod = context.HttpContext.Request.Method,
                Url = context.HttpContext.Request.Path.Value ?? "",
                IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                Timestamp = DateTime.UtcNow,
                IsSuccess = isSuccess,
                ExecutionTimeMs = _stopwatch.ElapsedMilliseconds
            };

            await _context.AuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        private string BuildActionDetails(ActionExecutingContext context)
        {
            var details = new List<string>();

            foreach (var param in context.ActionArguments)
            {
                // Пропускаем чувствительные данные
                if (param.Key.ToLower().Contains("password") || 
                    param.Key.ToLower().Contains("token") ||
                    param.Key.ToLower().Contains("secret"))
                {
                    details.Add($"{param.Key}=[HIDDEN]");
                    continue;
                }

                if (param.Value != null && param.Value.GetType().IsClass && 
                    param.Value.GetType() != typeof(string))
                {
                    details.Add($"{param.Key}=[{param.Value.GetType().Name}]");
                }
                else if (param.Value != null)
                {
                    var value = param.Value.ToString();
                    if (value?.Length > 50)
                    {
                        value = value.Substring(0, 47) + "...";
                    }
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