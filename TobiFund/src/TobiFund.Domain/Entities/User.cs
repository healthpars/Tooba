using TobiFund.Domain.Enums;

namespace TobiFund.Domain.Entities;

/// <summary>کاربر سیستم (مدیر/اپراتور)</summary>
public class User
{
    public int Id { get; set; }

    /// <summary>نام کاربری (یکتا)</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>هش رمز عبور (PBKDF2) - هرگز متن ساده ذخیره نمی‌شود</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Operator;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastLoginAt { get; set; }
}
