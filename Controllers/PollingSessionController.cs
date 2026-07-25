using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;

namespace JwtAuthApp.Controllers
{
    [Authorize]
    public class PollingSessionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PollingSessionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _context.PollingSessions
                .AsNoTracking()
                .Include(p => p.MonitoringPost)
                .OrderByDescending(p => p.StartedAt)
                .ToListAsync();
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var item = await _context.PollingSessions
                .AsNoTracking()
                .Include(p => p.MonitoringPost)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _context.PollingSessions
                .AsNoTracking()
                .Include(p => p.MonitoringPost)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var item = await _context.PollingSessions.FindAsync(id);
            if (item != null)
            {
                _context.PollingSessions.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Сессия опроса успешно удалена!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
