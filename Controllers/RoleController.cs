using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoleController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles
                .Include(r => r.UserRoles)
                .OrderBy(r => r.Name)
                .ToListAsync();
            return View(roles);
        }

        public IActionResult Create()
        {
            return View(new Role());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            if (await _context.Roles.AnyAsync(r => r.Name == role.Name))
            {
                ModelState.AddModelError("Name", "Role with this name already exists");
                return View(role);
            }

            if (ModelState.IsValid)
            {
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Role created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();
            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Role role)
        {
            if (id != role.Id) return NotFound();

            if (await _context.Roles.AnyAsync(r => r.Name == role.Name && r.Id != id))
            {
                ModelState.AddModelError("Name", "Role with this name already exists");
                return View(role);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Roles.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.Name = role.Name;
                    existing.Description = role.Description;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Role updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Roles.AnyAsync(r => r.Id == id))
                        return NotFound();
                    throw;
                }
            }
            return View(role);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                    .ThenInclude(ur => ur.User)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (role == null) return NotFound();
            return View(role);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role != null)
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Role deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
