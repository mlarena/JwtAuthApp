using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;
using System.Text.Json;

namespace JwtAuthApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ChangeLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChangeLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? entityType,
            string? changeType,
            DateTime? from,
            DateTime? to,
            int page = 1)
        {
            const int pageSize = 50;

            // Получаем списки для фильтров
            ViewBag.EntityTypes = await _context.EntityChangeLogs
                .Select(l => l.EntityType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            ViewBag.ChangeTypes = await _context.EntityChangeLogs
                .Select(l => l.ChangeType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            var query = _context.EntityChangeLogs
                .Include(l => l.User)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(l => l.EntityType == entityType);

            if (!string.IsNullOrEmpty(changeType))
                query = query.Where(l => l.ChangeType == changeType);

            if (from.HasValue)
                query = query.Where(l => l.Timestamp >= from.Value.ToUniversalTime());

            if (to.HasValue)
                query = query.Where(l => l.Timestamp <= to.Value.ToUniversalTime());

            query = query.OrderByDescending(l => l.Timestamp);

            var totalItems = await query.CountAsync();
            
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TotalItems = totalItems;
            ViewBag.EntityType = entityType;
            ViewBag.ChangeType = changeType;
            ViewBag.From = from;
            ViewBag.To = to;

            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var log = await _context.EntityChangeLogs
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (log == null)
                return NotFound();

            // Десериализуем JSON для отображения
            ViewBag.OriginalData = !string.IsNullOrEmpty(log.OriginalValues) 
                ? JsonSerializer.Deserialize<object>(log.OriginalValues) 
                : null;
            
            ViewBag.NewData = !string.IsNullOrEmpty(log.NewValues) 
                ? JsonSerializer.Deserialize<object>(log.NewValues) 
                : null;
            
            ViewBag.Changes = !string.IsNullOrEmpty(log.ChangedProperties) 
                ? JsonSerializer.Deserialize<object>(log.ChangedProperties) 
                : null;

            return View(log);
        }
    }
}