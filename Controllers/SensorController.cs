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
        private readonly ILogger<SensorController> _logger;

        public SensorController(ApplicationDbContext context, ILogger<SensorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var sensors = await _context.Sensors
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            return View(sensors);
        }

        public async Task<IActionResult> Details(int id)
        {
            var sensor = await _context.Sensors.FindAsync(id);
            if (sensor == null) return NotFound();
            return View(sensor);
        }

        public IActionResult Create()
        {
            return View(new Sensor());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sensor sensor)
        {
            if (sensor.MinValue.HasValue && sensor.MaxValue.HasValue && sensor.MinValue > sensor.MaxValue)
            {
                ModelState.AddModelError("", "Min value cannot be greater than Max value.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    sensor.CreatedAt = DateTime.UtcNow;
                    sensor.UpdatedAt = DateTime.UtcNow;
                    _context.Sensors.Add(sensor);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Sensor created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating sensor");
                    ModelState.AddModelError("", "An error occurred while saving the sensor.");
                }
            }
            return View(sensor);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var sensor = await _context.Sensors.FindAsync(id);
            if (sensor == null) return NotFound();
            return View(sensor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Sensor sensor)
        {
            if (id != sensor.Id) return NotFound();

            if (sensor.MinValue.HasValue && sensor.MaxValue.HasValue && sensor.MinValue > sensor.MaxValue)
            {
                ModelState.AddModelError("", "Min value cannot be greater than Max value.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Sensors.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.Name = sensor.Name;
                    existing.Type = sensor.Type;
                    existing.Unit = sensor.Unit;
                    existing.MinValue = sensor.MinValue;
                    existing.MaxValue = sensor.MaxValue;
                    existing.IsActive = sensor.IsActive;
                    existing.UpdatedAt = DateTime.UtcNow;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Sensor updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Sensors.AnyAsync(s => s.Id == id))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating sensor");
                    ModelState.AddModelError("", "An error occurred while updating the sensor.");
                }
            }
            return View(sensor);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var sensor = await _context.Sensors.FindAsync(id);
            if (sensor == null) return NotFound();
            return View(sensor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sensor = await _context.Sensors.FindAsync(id);
            if (sensor != null)
            {
                _context.Sensors.Remove(sensor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Sensor deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var sensor = await _context.Sensors.FindAsync(id);
            if (sensor != null)
            {
                sensor.IsActive = !sensor.IsActive;
                sensor.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Sensor {(sensor.IsActive ? "activated" : "deactivated")} successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
