using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Controllers
{
    [Authorize]
    public class DataIWSController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataIWSController> _logger;

        public DataIWSController(ApplicationDbContext context, ILogger<DataIWSController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? sensorId)
        {
            var query = _context.DataIWS
                .Include(d => d.Sensor)
                .AsQueryable();

            if (sensorId.HasValue)
            {
                query = query.Where(d => d.SensorId == sensorId.Value);
            }

            var data = await query
                .OrderByDescending(d => d.RecordedAt)
                .ToListAsync();

            ViewBag.Sensors = await _context.Sensors
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            ViewBag.SelectedSensorId = sensorId;

            return View(data);
        }

        public async Task<IActionResult> Details(int id)
        {
            var entry = await _context.DataIWS
                .Include(d => d.Sensor)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (entry == null) return NotFound();
            return View(entry);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Sensors = await _context.Sensors
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            return View(new DataIWS { RecordedAt = DateTime.UtcNow });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DataIWS entry)
        {
            if (entry.SensorId <= 0)
            {
                ModelState.AddModelError("SensorId", "Пожалуйста, выберите датчик.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    entry.CreatedAt = DateTime.UtcNow;
                    _context.DataIWS.Add(entry);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Запись данных успешно создана!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating data entry");
                    ModelState.AddModelError("", "Произошла ошибка при сохранении данных.");
                }
            }

            ViewBag.Sensors = await _context.Sensors
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            return View(entry);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var entry = await _context.DataIWS.FindAsync(id);
            if (entry == null) return NotFound();

            ViewBag.Sensors = await _context.Sensors
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            return View(entry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DataIWS entry)
        {
            if (id != entry.Id) return NotFound();

            if (entry.SensorId <= 0)
            {
                ModelState.AddModelError("SensorId", "Пожалуйста, выберите датчик.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.DataIWS.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.SensorId = entry.SensorId;
                    existing.Value = entry.Value;
                    existing.RecordedAt = entry.RecordedAt;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Запись данных успешно обновлена!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.DataIWS.AnyAsync(d => d.Id == id))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating data entry");
                    ModelState.AddModelError("", "Произошла ошибка при обновлении данных.");
                }
            }

            ViewBag.Sensors = await _context.Sensors
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            return View(entry);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var entry = await _context.DataIWS
                .Include(d => d.Sensor)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (entry == null) return NotFound();
            return View(entry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entry = await _context.DataIWS.FindAsync(id);
            if (entry != null)
            {
                _context.DataIWS.Remove(entry);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Запись данных успешно удалена!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
