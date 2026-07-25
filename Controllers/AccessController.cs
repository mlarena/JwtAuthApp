using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;
using JwtAuthApp.ViewModels;

namespace JwtAuthApp.Controllers
{
    [Authorize]
    public class AccessController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccessController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var accesses = await _context.ControllerAccesses
                .Include(c => c.ControllerAccessRoles)
                    .ThenInclude(cr => cr.Role)
                .OrderBy(c => c.ControllerName)
                .ToListAsync();

            ViewBag.AllRoles = await _context.Roles.OrderBy(r => r.Name).ToListAsync();

            return View(accesses);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var access = await _context.ControllerAccesses
                .Include(c => c.ControllerAccessRoles)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (access == null) return NotFound();

            var allRoles = await _context.Roles.OrderBy(r => r.Name).ToListAsync();
            var assignedRoleIds = access.ControllerAccessRoles.Select(cr => cr.RoleId).ToList();

            var vm = new ControllerAccessEditViewModel
            {
                Id = access.Id,
                ControllerName = access.ControllerName,
                DisplayName = access.DisplayName,
                Description = access.Description,
                AllowAllAuthenticated = access.AllowAllAuthenticated,
                Roles = allRoles.Select(r => new RoleCheckViewModel
                {
                    RoleId = r.Id,
                    RoleName = r.Name,
                    IsSelected = assignedRoleIds.Contains(r.Id)
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ControllerAccessEditViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var access = await _context.ControllerAccesses
                    .Include(c => c.ControllerAccessRoles)
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (access == null) return NotFound();

                access.DisplayName = vm.DisplayName;
                access.Description = vm.Description;
                access.AllowAllAuthenticated = vm.AllowAllAuthenticated;

                // Обновляем роли
                var currentRoles = access.ControllerAccessRoles.ToList();
                _context.ControllerAccessRoles.RemoveRange(currentRoles);

                foreach (var role in vm.Roles.Where(r => r.IsSelected))
                {
                    _context.ControllerAccessRoles.Add(new ControllerAccessRole
                    {
                        ControllerAccessId = id,
                        RoleId = role.RoleId
                    });
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Правила доступа для \"{access.DisplayName}\" обновлены!";
                return RedirectToAction(nameof(Index));
            }

            vm.Roles = vm.Roles ?? new List<RoleCheckViewModel>();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickToggle(int id, string roleName, bool addRole)
        {
            var access = await _context.ControllerAccesses
                .Include(c => c.ControllerAccessRoles)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (access == null) return NotFound();

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) return NotFound();

            if (addRole)
            {
                if (!access.ControllerAccessRoles.Any(cr => cr.RoleId == role.Id))
                {
                    _context.ControllerAccessRoles.Add(new ControllerAccessRole
                    {
                        ControllerAccessId = id,
                        RoleId = role.Id
                    });
                    access.AllowAllAuthenticated = false;
                }
            }
            else
            {
                var link = access.ControllerAccessRoles.FirstOrDefault(cr => cr.RoleId == role.Id);
                if (link != null)
                {
                    _context.ControllerAccessRoles.Remove(link);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Доступ для \"{access.DisplayName}\" обновлён!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAllowAll(int id)
        {
            var access = await _context.ControllerAccesses.FindAsync(id);
            if (access == null) return NotFound();

            access.AllowAllAuthenticated = !access.AllowAllAuthenticated;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"\"{access.DisplayName}\" — Разрешить всем установлено: {access.AllowAllAuthenticated}";
            return RedirectToAction(nameof(Index));
        }
    }
}
