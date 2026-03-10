using JwtAuthApp.Data;
using JwtAuthApp.Models;
using JwtAuthApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthApp.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AuditController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        AuditLogType? type,
        string? userName,
        string? ipAddress,
        string? entityType,
        string? changeType,
        DateTime? from,
        DateTime? to,
        int page = 1)
    {
        const int pageSize = 50;
        page = Math.Max(1, page);

        // Convert UI date range to UTC for storage comparison (storage uses UTC)
        DateTime? fromUtc = from?.ToUniversalTime();
        DateTime? toUtc = to?.ToUniversalTime();

        // Build dropdown lists (must be sequential on single DbContext)
        var userNames = await _context.AuditLogs
            .AsNoTracking()
            .Where(l => l.UserName != null)
            .Select(l => l.UserName!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var ipAddresses = await _context.AuditLogs
            .AsNoTracking()
            .Where(l => l.Type == AuditLogType.Action && l.IpAddress != null)
            .Select(l => l.IpAddress!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var entityTypes = await _context.AuditLogs
            .AsNoTracking()
            .Where(l => l.Type == AuditLogType.Change && l.EntityType != null)
            .Select(l => l.EntityType!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var changeTypes = await _context.AuditLogs
            .AsNoTracking()
            .Where(l => l.Type == AuditLogType.Change && l.ChangeType != null)
            .Select(l => l.ChangeType!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        // Common filters
        if (type.HasValue)
        {
            query = query.Where(l => l.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            query = query.Where(l => l.UserName == userName);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(l => l.Timestamp >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(l => l.Timestamp <= toUtc.Value);
        }

        // Type-specific filters
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            query = query.Where(l => l.Type == AuditLogType.Action && l.IpAddress == ipAddress);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(l => l.Type == AuditLogType.Change && l.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(changeType))
        {
            query = query.Where(l => l.Type == AuditLogType.Change && l.ChangeType == changeType);
        }

        int totalItems = await query.CountAsync();

        var merged = await query
            .OrderByDescending(l => l.Timestamp)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogRowViewModel
            {
                Type = l.Type,
                Id = l.Id,
                TimestampUtc = l.Timestamp,
                UserName = l.UserName,
                UserId = l.UserId,

                ActionName = l.Action,
                HttpMethod = l.HttpMethod,
                Url = l.Url,
                IpAddress = l.IpAddress,
                IsSuccess = l.IsSuccess,
                ExecutionTimeMs = l.ExecutionTimeMs,
                Details = l.Details,
                TargetId = l.TargetId,

                EntityType = l.EntityType,
                EntityId = l.EntityId,
                ChangeType = l.ChangeType
            })
            .ToListAsync();

        var vm = new AuditIndexViewModel
        {
            Items = merged,

            Type = type,
            UserName = userName,
            IpAddress = ipAddress,
            EntityType = entityType,
            ChangeType = changeType,
            From = from,
            To = to,

            UserNames = userNames,
            IpAddresses = ipAddresses,
            EntityTypes = entityTypes,
            ChangeTypes = changeTypes,

            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(AuditLogType type, int id)
    {
        if (id <= 0) return NotFound();

        var vm = new AuditDetailsViewModel
        {
            Type = type,
            Id = id
        };

        vm.Log = await _context.AuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.Type == type);
        if (vm.Log is null) return NotFound();

        return View(vm);
    }
}

