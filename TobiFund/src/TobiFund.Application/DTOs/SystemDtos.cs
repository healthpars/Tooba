namespace TobiFund.Application.DTOs;

public class SettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AuditLogDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedAtPersian { get; set; } = string.Empty;
}
