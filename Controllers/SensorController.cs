using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Controllers
{
    [Authorize]
    public class SensorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SensorController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _context.Sensors
                .AsNoTracking()
                .Include(s => s.SensorType)
                .Include(s => s.MonitoringPost)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.SensorTypes = await _context.SensorTypes
                .AsNoTracking()
                .OrderBy(t => t.SensorTypeName)
                .ToListAsync();
            ViewBag.MonitoringPosts = await _context.MonitoringPosts
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sensor model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.UtcNow;
                _context.Sensors.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Датчик успешно создан!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SensorTypes = await _context.SensorTypes
                .AsNoTracking()
                .OrderBy(t => t.SensorTypeName)
                .ToListAsync();
            ViewBag.MonitoringPosts = await _context.MonitoringPosts
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.Sensors.FindAsync(id);
            if (item == null) return NotFound();
            ViewBag.SensorTypes = await _context.SensorTypes
                .AsNoTracking()
                .OrderBy(t => t.SensorTypeName)
                .ToListAsync();
            ViewBag.MonitoringPosts = await _context.MonitoringPosts
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Sensor model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Sensors.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.SensorTypeId = model.SensorTypeId;
                    existing.MonitoringPostId = model.MonitoringPostId;
                    existing.Longitude = model.Longitude;
                    existing.Latitude = model.Latitude;
                    existing.SerialNumber = model.SerialNumber;
                    existing.EndPointsName = model.EndPointsName;
                    existing.Url = model.Url;
                    existing.IsActive = model.IsActive;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Датчик успешно обновлён!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Sensors.AnyAsync(s => s.Id == id))
                        return NotFound();
                    throw;
                }
            }
            ViewBag.SensorTypes = await _context.SensorTypes
                .AsNoTracking()
                .OrderBy(t => t.SensorTypeName)
                .ToListAsync();
            ViewBag.MonitoringPosts = await _context.MonitoringPosts
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.Sensors
                .AsNoTracking()
                .Include(s => s.SensorType)
                .Include(s => s.MonitoringPost)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Sensors
                .AsNoTracking()
                .Include(s => s.SensorType)
                .Include(s => s.MonitoringPost)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Sensors.FindAsync(id);
            if (item != null)
            {
                _context.Sensors.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Датчик успешно удалён!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var item = await _context.Sensors.FindAsync(id);
            if (item == null) return NotFound();

            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();

            var status = item.IsActive ? "активирован" : "деактивирован";
            TempData["Success"] = $"Датчик \"{item.SerialNumber}\" {status}!";
            return RedirectToAction(nameof(Index));
        }
    }
}
