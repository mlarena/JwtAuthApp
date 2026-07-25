using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

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

            // Защищённая
            var secure = allAccess.FirstOrDefault(c => c.ControllerName == "Secure");
            if (secure != null && CanAccess(secure, userRoles))
                items.Add(new MenuItemViewModel { Controller = "Secure", Action = "Index", Text = "Защищённая", Icon = "bi-lock" });

            // Тест
            var test = allAccess.FirstOrDefault(c => c.ControllerName == "Test");
            if (test != null && CanAccess(test, userRoles))
                items.Add(new MenuItemViewModel { Controller = "Test", Action = "Index", Text = "Тест", Icon = "bi-bug" });

            // Посты мониторинга
            var monitoring = allAccess.FirstOrDefault(c => c.ControllerName == "MonitoringPost");
            if (monitoring != null && CanAccess(monitoring, userRoles))
                items.Add(new MenuItemViewModel { Controller = "MonitoringPost", Action = "Index", Text = "Мониторинг", Icon = "bi-geo-alt" });

            // Датчики
            var sensor = allAccess.FirstOrDefault(c => c.ControllerName == "Sensor");
            if (sensor != null && CanAccess(sensor, userRoles))
                items.Add(new MenuItemViewModel { Controller = "Sensor", Action = "Index", Text = "Датчики", Icon = "bi-cpu" });

            // Данные IWS
            var dataIws = allAccess.FirstOrDefault(c => c.ControllerName == "DataIWS");
            if (dataIws != null && CanAccess(dataIws, userRoles))
                items.Add(new MenuItemViewModel { Controller = "DataIWS", Action = "Index", Text = "Данные IWS", Icon = "bi-database" });

            // Администрирование
            if (userRoles.Contains("Admin"))
            {
                var adminItems = new List<MenuItemViewModel>();

                var adminAccess = allAccess.FirstOrDefault(c => c.ControllerName == "Admin");
                if (adminAccess != null)
                    adminItems.Add(new MenuItemViewModel { Controller = "Admin", Action = "Index", Text = "Пользователи", Icon = "bi-people" });

                var roleAccess = allAccess.FirstOrDefault(c => c.ControllerName == "Role");
                if (roleAccess != null)
                    adminItems.Add(new MenuItemViewModel { Controller = "Role", Action = "Index", Text = "Роли", Icon = "bi-shield-lock" });

                var accessAccess = allAccess.FirstOrDefault(c => c.ControllerName == "Access");
                if (accessAccess != null)
                    adminItems.Add(new MenuItemViewModel { Controller = "Access", Action = "Index", Text = "Управление доступом", Icon = "bi-key" });

                var auditAccess = allAccess.FirstOrDefault(c => c.ControllerName == "Audit");
                if (auditAccess != null)
                    adminItems.Add(new MenuItemViewModel { Controller = "Audit", Action = "Index", Text = "Журнал аудита", Icon = "bi-journal-text" });

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
