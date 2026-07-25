using System.Reflection;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Services
{
    public static class ControllerDiscoveryService
    {
        private static readonly HashSet<string> ExemptControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Auth", "Home", "Error"
        };

        public static void DiscoverAndRegister(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var controllers = GetControllerNames();
            var existingNames = db.ControllerAccesses.Select(c => c.ControllerName).ToHashSet();

            var newControllers = controllers
                .Where(c => !existingNames.Contains(c) && !ExemptControllers.Contains(c))
                .ToList();

            if (!newControllers.Any()) return;

            foreach (var name in newControllers)
            {
                db.ControllerAccesses.Add(new ControllerAccess
                {
                    ControllerName = name,
                    DisplayName = name,
                    Description = null,
                    AllowAllAuthenticated = false
                });
            }

            db.SaveChanges();
        }

        private static List<string> GetControllerNames()
        {
            var controllers = new List<string>();

            var assembly = Assembly.GetExecutingAssembly();
            var controllerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"));

            foreach (var type in controllerTypes)
            {
                var name = type.Name.Replace("Controller", "");
                controllers.Add(name);
            }

            return controllers;
        }
    }
}
