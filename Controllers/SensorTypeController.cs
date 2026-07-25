using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Controllers
{
    [Authorize]
    public class SensorTypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SensorTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _context.SensorTypes
                .AsNoTracking()
                .OrderBy(t => t.SensorTypeName)
                .ToListAsync();
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SensorType model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.UtcNow;
                _context.SensorTypes.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Тип датчика успешно создан!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.SensorTypes.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SensorType model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.SensorTypes.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.SensorTypeName = model.SensorTypeName;
                    existing.Description = model.Description;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Тип датчика успешно обновлён!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.SensorTypes.AnyAsync(t => t.Id == id))
                        return NotFound();
                    throw;
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.SensorTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.SensorTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.SensorTypes.FindAsync(id);
            if (item != null)
            {
                _context.SensorTypes.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Тип датчика успешно удалён!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
