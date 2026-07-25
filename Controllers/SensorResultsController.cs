using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Controllers
{
    [Authorize]
    public class SensorResultsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SensorResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? sensorId, int page = 1)
        {
            const int pageSize = 50;
            page = Math.Max(1, page);

            ViewBag.Sensors = await _context.Sensors
                .AsNoTracking()
                .Include(s => s.SensorType)
                .OrderBy(s => s.SerialNumber)
                .ToListAsync();
            ViewBag.SelectedSensorId = sensorId;

            var query = _context.SensorResults
                .AsNoTracking()
                .Include(r => r.Sensor)
                .AsQueryable();

            if (sensorId.HasValue)
            {
                query = query.Where(r => r.SensorId == sensorId.Value);
            }

            int totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CheckedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.SensorResults
                .AsNoTracking()
                .Include(r => r.Sensor)
                .Include(r => r.PollingSession)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }
    }
}
