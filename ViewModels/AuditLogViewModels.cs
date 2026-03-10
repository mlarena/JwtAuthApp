using JwtAuthApp.Models;

namespace JwtAuthApp.ViewModels;

public sealed class AuditLogRowViewModel
{
    public required AuditLogType Type { get; init; }
    public required int Id { get; init; }
    public required DateTime TimestampUtc { get; init; }

    public string? UserName { get; init; }
    public int? UserId { get; init; }

    // Action log fields
    public string? ActionName { get; init; }         // e.g. Home.Index
    public string? HttpMethod { get; init; }
    public string? Url { get; init; }
    public string? IpAddress { get; init; }
    public bool? IsSuccess { get; init; }
    public long? ExecutionTimeMs { get; init; }
    public string? Details { get; init; }
    public int? TargetId { get; init; }

    // Change log fields
    public string? EntityType { get; init; }
    public int? EntityId { get; init; }
    public string? ChangeType { get; init; }         // Added/Modified/Deleted
}

public sealed class AuditIndexViewModel
{
    public IReadOnlyList<AuditLogRowViewModel> Items { get; init; } = Array.Empty<AuditLogRowViewModel>();

    // Filters
    public AuditLogType? Type { get; init; }
    public string? UserName { get; init; }
    public string? IpAddress { get; init; }
    public string? EntityType { get; init; }
    public string? ChangeType { get; init; }
    public DateTime? From { get; init; } // interpreted as local on UI, converted in controller
    public DateTime? To { get; init; }   // interpreted as local on UI, converted in controller

    // Dropdown lists
    public IReadOnlyList<string> UserNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> IpAddresses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EntityTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ChangeTypes { get; init; } = Array.Empty<string>();

    // Paging
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalItems { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
}

public sealed class AuditDetailsViewModel
{
    public required AuditLogType Type { get; init; }
    public required int Id { get; init; }

    public AuditLog? Log { get; set; }
}

