namespace TobiFund.Domain.Entities;

/// <summary>تنظیمات سیستم (Key-Value)</summary>
public class Setting
{
    public int Id { get; set; }

    /// <summary>کلید تنظیم (یکتا)</summary>
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string? UpdatedBy { get; set; }
}
