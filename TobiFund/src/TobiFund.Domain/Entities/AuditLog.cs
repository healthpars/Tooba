namespace TobiFund.Domain.Entities;

/// <summary>Audit Log - ثبت رویدادهای مهم سیستم</summary>
public class AuditLog
{
    public int Id { get; set; }

    /// <summary>نام کاربری انجام‌دهنده عملیات</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>نوع عملیات (Login, MemberCreated, ...) </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>نوع موجودیت</summary>
    public string? EntityType { get; set; }

    /// <summary>شناسه موجودیت</summary>
    public string? EntityId { get; set; }

    /// <summary>توضیحات (مثلاً: عضو 1001 وام 200,000 ثبت کرد)</summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
