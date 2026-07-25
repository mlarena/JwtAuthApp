using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;
using JwtAuthApp.ViewModels;
using JwtAuthApp.Services;

namespace JwtAuthApp.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public AdminController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();

            ViewBag.AllRoles = await _context.Roles.OrderBy(r => r.Name).ToListAsync();

            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.UserName == viewModel.UserName))
                {
                    ModelState.AddModelError("UserName", "Username already exists");
                    return View(viewModel);
                }

                var (hash, salt) = _authService.HashPassword(viewModel.Password);
                var user = new User
                {
                    UserName = viewModel.UserName,
                    PasswordHash = hash,
                    Salt = salt,
                    Role = "User"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Назначаем роль "User" по умолчанию
                var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                if (defaultRole != null)
                {
                    _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = defaultRole.Id });
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null) return NotFound();

                    existingUser.UserName = viewModel.UserName;
                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Users.AnyAsync(u => u.Id == id))
                        return NotFound();
                    throw;
                }
            }
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Управление ролями пользователя
        [HttpGet]
        public async Task<IActionResult> ManageRoles(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var allRoles = await _context.Roles.OrderBy(r => r.Name).ToListAsync();
            var userRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();

            var viewModel = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                AllRoles = allRoles.Select(r => new RoleCheckBoxViewModel
                {
                    RoleId = r.Id,
                    RoleName = r.Name,
                    IsSelected = userRoleIds.Contains(r.Id)
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(int id, ManageUserRolesViewModel viewModel)
        {
            if (id != viewModel.UserId) return NotFound();

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            // Удаляем текущие роли
            var currentRoles = user.UserRoles.ToList();
            _context.UserRoles.RemoveRange(currentRoles);

            // Добавляем выбранные роли
            foreach (var roleCheck in viewModel.AllRoles.Where(r => r.IsSelected))
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = id,
                    RoleId = roleCheck.RoleId
                });
            }

            // Обновляем поле Role для обратной совместимости
            var selectedRoleNames = viewModel.AllRoles
                .Where(r => r.IsSelected)
                .Select(r => r.RoleName)
                .ToList();
            user.Role = selectedRoleNames.FirstOrDefault() ?? "User";

            await _context.SaveChangesAsync();
            TempData["Success"] = "User roles updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAddRole(int userId, string roleName)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) return NotFound();

            if (!user.UserRoles.Any(ur => ur.RoleId == role.Id))
            {
                _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
                user.Role = roleName;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Role \"{roleName}\" added to {user.UserName}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickRemoveRole(int userId, string roleName)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) return NotFound();

            var userRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == role.Id);
            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);

                // Обновляем поле Role
                var remainingRoles = user.UserRoles.Where(ur => ur.RoleId != role.Id).ToList();
                user.Role = remainingRoles.FirstOrDefault()?.Role?.Name ?? "User";

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Role \"{roleName}\" removed from {user.UserName}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Смена пароля
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(new ChangePasswordViewModel
            {
                UserId = user.Id,
                UserName = user.UserName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, ChangePasswordViewModel viewModel)
        {
            if (id != viewModel.UserId) return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound();

                var (hash, salt) = _authService.HashPassword(viewModel.NewPassword);
                user.PasswordHash = hash;
                user.Salt = salt;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Password for \"{user.UserName}\" changed successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // Блокировка / разблокировка
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsBlocked = !user.IsBlocked;
            await _context.SaveChangesAsync();

            var status = user.IsBlocked ? "blocked" : "unblocked";
            TempData["Success"] = $"User \"{user.UserName}\" has been {status}!";

            return RedirectToAction(nameof(Index));
        }
    }
}
