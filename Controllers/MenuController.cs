using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApp.Services;
using System.Security.Claims;

namespace JwtAuthApp.Controllers
{
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ControllerDiscoveryService _controllerDiscoveryService;

        public MenuController(ApplicationDbContext context, ControllerDiscoveryService controllerDiscoveryService)
        {
            _context = context;
            _controllerDiscoveryService = controllerDiscoveryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserMenuItems()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new string[] { });
            }

            var userName = User.Identity.Name;
            var isSuperUser = userName.Equals("su", StringComparison.OrdinalIgnoreCase);

            if (isSuperUser)
            {
                var allControllers = _controllerDiscoveryService.GetControllerNames();
                return Json(allControllers);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var userControllerAccesses = await _context.UserControllerAccesses
                .Where(uca => uca.UserId == userId)
                .Select(uca => uca.ControllerName)
                .ToListAsync();

            if (userControllerAccesses.Count == 0)
            {
                return Json(new string[] { });
            }

            return Json(userControllerAccesses);
        }
    }
}
