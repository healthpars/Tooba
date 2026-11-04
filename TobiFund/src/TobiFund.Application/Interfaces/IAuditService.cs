using TobiFund.Application.DTOs;

namespace TobiFund.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null, string? description = null, string? username = null);
    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int count = 200);
    Task<IReadOnlyList<AuditLogDto>> GetAllAsync(DateTime? from = null, DateTime? to = null);
}
