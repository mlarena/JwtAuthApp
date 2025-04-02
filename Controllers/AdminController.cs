using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;
using JwtAuthApp.Services;
using JwtAuthApp.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ControllerDiscoveryService _controllerDiscoveryService;

        public AdminController(ApplicationDbContext context, ControllerDiscoveryService controllerDiscoveryService)
        {
            _context = context;
            _controllerDiscoveryService = controllerDiscoveryService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var controllers = _controllerDiscoveryService.GetControllerNames();
            var userControllerAccesses = await _context.UserControllerAccesses
                .Where(uca => uca.UserId == id)
                .Select(uca => uca.ControllerName)
                .ToListAsync();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                SelectedControllers = userControllerAccesses
            };

            ViewBag.Controllers = controllers;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    existingUser.Username = viewModel.Username;
                    existingUser.Role = viewModel.Role;
                    _context.Update(existingUser);

                    // Обновить доступы к контроллерам
                    var currentAccesses = await _context.UserControllerAccesses
                        .Where(uca => uca.UserId == id)
                        .ToListAsync();

                    _context.UserControllerAccesses.RemoveRange(currentAccesses);

                    foreach (var controller in viewModel.SelectedControllers)
                    {
                        _context.UserControllerAccesses.Add(new UserControllerAccess
                        {
                            UserId = id,
                            ControllerName = controller
                        });
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Логирование ошибки
                    Console.WriteLine($"DbUpdateConcurrencyException: {ex.Message}");
                    if (!UserExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    // Логирование ошибки
                    Console.WriteLine($"Exception: {ex.Message}");
                    throw;
                }
            }

            // Логирование ошибок валидации
            foreach (var modelState in ModelState.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
            }

            // Если ModelState недействителен, вернуть представление с текущими данными
            var controllers = _controllerDiscoveryService.GetControllerNames();
            ViewBag.Controllers = controllers;

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

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
