using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Controllers
{
    [Authorize(Roles = "Admin")] // Только админы могут смотреть логи
    public class LogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? userName, 
            string? ipAddress,
            DateTime? from, 
            DateTime? to,
            int page = 1)
        {
            const int pageSize = 50;

            // Получаем списки уникальных значений для фильтров
            var userNames = await _context.UserActionLogs
                .Where(l => l.UserName != null)
                .Select(l => l.UserName)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();

            var ipAddresses = await _context.UserActionLogs
                .Where(l => l.IpAddress != null)
                .Select(l => l.IpAddress)
                .Distinct()
                .OrderBy(ip => ip)
                .ToListAsync();

            // Сохраняем списки в ViewBag для выпадающих списков
            ViewBag.UserNames = userNames;
            ViewBag.IpAddresses = ipAddresses;

            // Начинаем с базового запроса
            var query = _context.UserActionLogs
                .Include(l => l.User)
                .AsNoTracking()
                .AsQueryable();

            // Применяем фильтры
            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(l => l.UserName == userName);
            }

            if (!string.IsNullOrEmpty(ipAddress))
            {
                query = query.Where(l => l.IpAddress == ipAddress);
            }

            if (from.HasValue)
            {
                var fromUtc = from.Value.ToUniversalTime();
                query = query.Where(l => l.Timestamp >= fromUtc);
            }

            if (to.HasValue)
            {
                var toUtc = to.Value.ToUniversalTime();
                query = query.Where(l => l.Timestamp <= toUtc);
            }

            // Сортировка по убыванию времени
            query = query.OrderByDescending(l => l.Timestamp);

            // Получаем общее количество записей
            var totalItems = await query.CountAsync();
            
            // Применяем пагинацию
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Передаем данные в View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TotalItems = totalItems;
            ViewBag.UserName = userName;
            ViewBag.IpAddress = ipAddress;
            ViewBag.From = from;
            ViewBag.To = to;

            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var log = await _context.UserActionLogs
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (log == null)
                return NotFound();

            return View(log);
        }
    }
}