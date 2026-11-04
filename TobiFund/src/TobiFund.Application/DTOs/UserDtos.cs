using TobiFund.Domain.Enums;

namespace TobiFund.Application.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string RoleName => Role == UserRole.Admin ? "مدیر" : "اپراتور";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AuthResultDto
{
    public UserDto User { get; set; } = null!;

    /// <summary>true یعنی کاربر هنوز از رمز عبور پیش‌فرض استفاده می‌کند</summary>
    public bool IsDefaultPassword { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Operator;
    public string Password { get; set; } = string.Empty;
}
