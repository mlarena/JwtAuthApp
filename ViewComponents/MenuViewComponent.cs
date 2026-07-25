using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        private static readonly HashSet<string> HiddenControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Auth", "Home"
        };

        private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Secure"] = "Защищённая",
            ["Test"] = "Тест",
            ["MonitoringPost"] = "Посты мониторинга",
            ["Sensor"] = "Датчики",
            ["SensorType"] = "Типы датчиков",
            ["DOVData"] = "Данные DOV",
            ["DSPDData"] = "Данные DSPD",
            ["DustData"] = "Данные пыли",
            ["IWSData"] = "Метеоданные IWS",
            ["MUEKSData"] = "Данные MUEKS",
            ["PollingSession"] = "Сессии опроса",
            ["SensorResults"] = "Результаты опроса",
            ["Admin"] = "Пользователи",
            ["Role"] = "Роли",
            ["Access"] = "Управление доступом",
            ["Audit"] = "Журнал аудита",
        };

        private static readonly Dictionary<string, string> Icons = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Secure"] = "bi-lock",
            ["Test"] = "bi-bug",
            ["MonitoringPost"] = "bi-geo-alt",
            ["Sensor"] = "bi-cpu",
            ["SensorType"] = "bi-tags",
            ["DOVData"] = "bi-eye",
            ["DSPDData"] = "bi-road",
            ["DustData"] = "bi-cloud-haze",
            ["IWSData"] = "bi-cloud-sun",
            ["MUEKSData"] = "bi-battery-charging",
            ["PollingSession"] = "bi-arrow-clockwise",
            ["SensorResults"] = "bi-clipboard-check",
            ["Admin"] = "bi-people",
            ["Role"] = "bi-shield-lock",
            ["Access"] = "bi-key",
            ["Audit"] = "bi-journal-text",
        };

        private static readonly string[] AdminControllers = { "Admin", "Role", "Access", "Audit" };

        public MenuViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userRoles = new List<string>();
            if (UserClaimsPrincipal.Identity?.IsAuthenticated == true)
            {
                userRoles = UserClaimsPrincipal.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();
            }

            var allAccess = await _context.ControllerAccesses
                .Include(c => c.ControllerAccessRoles)
                    .ThenInclude(cr => cr.Role)
                .OrderBy(c => c.ControllerName)
                .ToListAsync();

            var items = new List<MenuItemViewModel>();

            // Главная — всегда
            items.Add(new MenuItemViewModel { Controller = "Home", Action = "Index", Text = "Главная", Icon = "bi-house" });

            // Обычные пункты меню (не Admin-раздел)
            foreach (var access in allAccess)
            {
                if (HiddenControllers.Contains(access.ControllerName))
                    continue;
                if (AdminControllers.Contains(access.ControllerName))
                    continue;

                if (!CanAccess(access, userRoles))
                    continue;

                items.Add(new MenuItemViewModel
                {
                    Controller = access.ControllerName,
                    Action = "Index",
                    Text = DisplayNames.TryGetValue(access.ControllerName, out var name) ? name : access.DisplayName,
                    Icon = Icons.TryGetValue(access.ControllerName, out var icon) ? icon : "bi-folder"
                });
            }

            // Администрирование — раскрывающийся раздел
            if (userRoles.Contains("Admin"))
            {
                var adminItems = new List<MenuItemViewModel>();
                foreach (var access in allAccess)
                {
                    if (!AdminControllers.Contains(access.ControllerName))
                        continue;

                    if (!CanAccess(access, userRoles))
                        continue;

                    adminItems.Add(new MenuItemViewModel
                    {
                        Controller = access.ControllerName,
                        Action = "Index",
                        Text = DisplayNames.TryGetValue(access.ControllerName, out var name) ? name : access.DisplayName,
                        Icon = Icons.TryGetValue(access.ControllerName, out var icon) ? icon : "bi-gear"
                    });
                }

                if (adminItems.Any())
                {
                    items.Add(new MenuItemViewModel
                    {
                        Text = "Администрирование",
                        Icon = "bi-gear",
                        IsDropdown = true,
                        Children = adminItems
                    });
                }
            }

            return View(items);
        }

        private bool CanAccess(ControllerAccess access, List<string> userRoles)
        {
            if (access.AllowAllAuthenticated) return true;
            if (!access.ControllerAccessRoles.Any()) return true;
            return userRoles.Any(ur => access.ControllerAccessRoles.Any(cr => cr.Role.Name == ur));
        }
    }

    public class MenuItemViewModel
    {
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool IsDropdown { get; set; }
        public List<MenuItemViewModel> Children { get; set; } = new();
    }
}
