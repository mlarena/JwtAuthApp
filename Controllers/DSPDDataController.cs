using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Controllers
{
    [Authorize]
    public class DSPDDataController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DSPDDataController(ApplicationDbContext context)
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

            var query = _context.DSPDDatas
                .AsNoTracking()
                .Include(d => d.Sensor)
                .Include(d => d.MonitoringPost)
                .AsQueryable();

            if (sensorId.HasValue)
            {
                query = query.Where(d => d.SensorId == sensorId.Value);
            }

            int totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.DataTimestamp)
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
            var item = await _context.DSPDDatas
                .AsNoTracking()
                .Include(d => d.Sensor)
                .Include(d => d.PollingSession)
                .Include(d => d.MonitoringPost)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }
    }
}
