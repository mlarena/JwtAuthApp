using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApp.Controllers
{
    [Authorize]
    public class MonitoringPostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MonitoringPostController> _logger;

        public MonitoringPostController(ApplicationDbContext context, ILogger<MonitoringPostController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: MonitoringPost
        public async Task<IActionResult> Index()
        {
            var posts = await _context.MonitoringPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(posts);
        }

        // GET: MonitoringPost/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.MonitoringPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        // GET: MonitoringPost/Create
        public IActionResult Create()
        {
            return View(new MonitoringPost());
        }

        // POST: MonitoringPost/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MonitoringPost post)
        {
            // Проверяем валидность координат (оба null или оба заполнены)
            if ((post.Longitude == null && post.Latitude != null) || 
                (post.Longitude != null && post.Latitude == null))
            {
                ModelState.AddModelError("", "Both coordinates must be either provided or both empty.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    post.CreatedAt = DateTime.UtcNow;
                    post.UpdatedAt = DateTime.UtcNow;

                    _context.MonitoringPosts.Add(post);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Monitoring post created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating monitoring post");
                    ModelState.AddModelError("", "An error occurred while saving the post.");
                }
            }

            return View(post);
        }

        // GET: MonitoringPost/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.MonitoringPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        // POST: MonitoringPost/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MonitoringPost post)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            // Проверяем валидность координат
            if ((post.Longitude == null && post.Latitude != null) || 
                (post.Longitude != null && post.Latitude == null))
            {
                ModelState.AddModelError("", "Both coordinates must be either provided or both empty.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPost = await _context.MonitoringPosts.FindAsync(id);
                    if (existingPost == null)
                    {
                        return NotFound();
                    }

                    // Обновляем поля
                    existingPost.Name = post.Name;
                    existingPost.Description = post.Description;
                    existingPost.Longitude = post.Longitude;
                    existingPost.Latitude = post.Latitude;
                    existingPost.IsMobile = post.IsMobile;
                    existingPost.IsActive = post.IsActive;
                    existingPost.UpdatedAt = DateTime.UtcNow;

                    _context.Update(existingPost);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Monitoring post updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MonitoringPostExists(post.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating monitoring post");
                    ModelState.AddModelError("", "An error occurred while updating the post.");
                }
            }

            return View(post);
        }

        // GET: MonitoringPost/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.MonitoringPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        // POST: MonitoringPost/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.MonitoringPosts.FindAsync(id);
            if (post != null)
            {
                _context.MonitoringPosts.Remove(post);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Monitoring post deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: MonitoringPost/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var post = await _context.MonitoringPosts.FindAsync(id);
            if (post != null)
            {
                post.IsActive = !post.IsActive;
                post.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Post {(post.IsActive ? "activated" : "deactivated")} successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MonitoringPostExists(int id)
        {
            return _context.MonitoringPosts.Any(e => e.Id == id);
        }
    }
}