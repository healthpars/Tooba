using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Entities;

namespace TobiFund.Application.Services;

/// <summary>ثبت رویدادهای مهم سیستم (Audit Log)</summary>
public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAppLogger _logger;

    public AuditService(IApplicationDbContext context, ICurrentUserService currentUser, IAppLogger logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task LogAsync(string action, string? entityType = null, string? entityId = null, string? description = null, string? username = null)
    {
        try
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Username = username ?? _currentUser.UserName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // لاگ Audit نباید عملکرد برنامه را مختل کند
            _logger.Error("خطا در ثبت Audit Log", ex);
        }
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int count = 200)
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Take(count)
            .ToListAsync();
        return logs.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAllAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs.AsQueryable();
        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt < to.Value.Date.AddDays(1));

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .ToListAsync();
        return logs.Select(MapToDto).ToList();
    }

    private static AuditLogDto MapToDto(AuditLog a) => new()
    {
        Id = a.Id,
        Username = a.Username,
        Action = a.Action,
        EntityType = a.EntityType,
        EntityId = a.EntityId,
        Description = a.Description,
        CreatedAt = a.CreatedAt,
        CreatedAtPersian = PersianDateHelper.ToPersianDateTime(a.CreatedAt)
    };
}
