using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Entities;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Services;

/// <summary>احراز هویت و مدیریت کاربران (RBAC)</summary>
public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly IAppLogger _logger;

    public AuthService(IApplicationDbContext context, IAuditService audit, IAppLogger logger)
    {
        _context = context;
        _audit = audit;
        _logger = logger;
    }

    public string HashPassword(string password) => PasswordHasher.Hash(password);

    public async Task<ServiceResult<AuthResultDto>> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return ServiceResult<AuthResultDto>.Fail("نام کاربری و رمز عبور را وارد کنید.");

        // نام کاربری: حذف khoảng خالی و بدون حساسیت به حروف بزرگ/کوچک
        // رمز عبور: تبدیل ارقام فارسی/عربی به انگلیسی (سازگاری با کیبورد فارسی)
        var normalizedUsername = username.Trim();
        var normalizedPassword = PersianDateHelper.ToEnglishDigits(password);

        var users = await _context.Users.Where(u => !u.IsDeleted).ToListAsync();
        var user = users.FirstOrDefault(u =>
            string.Equals(u.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase));

        if (user is null || !user.IsActive || !PasswordHasher.Verify(normalizedPassword, user.PasswordHash))
        {
            _logger.Warning($"تلاش ناموفق برای ورود با نام کاربری '{normalizedUsername}'.");
            return ServiceResult<AuthResultDto>.Fail("نام کاربری یا رمز عبور صحیح نیست.");
        }

        user.LastLoginAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await _audit.LogAsync(AppConstants.AuditActions.Login, "User", user.Id.ToString(),
            $"کاربر {user.Username} وارد سیستم شد.", username: user.Username);

        return ServiceResult<AuthResultDto>.Ok(new AuthResultDto
        {
            User = MapToDto(user),
            IsDefaultPassword = user.Username == AppConstants.DefaultAdminUsername
                                && normalizedPassword == AppConstants.DefaultAdminPassword
        });
    }

    public async Task<ServiceResult> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        // تبدیل ارقام فارسی/عربی به انگلیسی در هر دو رمز (سازگاری با کیبورد فارسی)
        var normalizedOld = PersianDateHelper.ToEnglishDigits(oldPassword);
        var normalizedNew = PersianDateHelper.ToEnglishDigits(newPassword);

        if (string.IsNullOrWhiteSpace(normalizedNew) || normalizedNew.Length < 6)
            return ServiceResult.Fail("رمز عبور جدید باید حداقل ۶ کاراکتر باشد.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user is null)
            return ServiceResult.Fail("کاربر یافت نشد.");

        if (!PasswordHasher.Verify(normalizedOld, user.PasswordHash))
            return ServiceResult.Fail("رمز عبور فعلی صحیح نیست.");

        if (PasswordHasher.Verify(normalizedNew, user.PasswordHash))
            return ServiceResult.Fail("رمز عبور جدید نباید با رمز عبور فعلی یکسان باشد.");

        user.PasswordHash = PasswordHasher.Hash(normalizedNew);
        await _context.SaveChangesAsync();
        await _audit.LogAsync(AppConstants.AuditActions.PasswordChanged, "User", user.Id.ToString(),
            $"رمز عبور کاربر {user.Username} تغییر کرد.");
        return ServiceResult.Ok("رمز عبور با موفقیت تغییر کرد.");
    }

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
            return ServiceResult<UserDto>.Fail("نام کاربری الزامی است.");
        // تبدیل ارقام فارسی/عربی به انگلیسی (سازگاری با کیبورد فارسی)
        var normalizedPassword = PersianDateHelper.ToEnglishDigits(dto.Password);
        if (string.IsNullOrWhiteSpace(normalizedPassword) || normalizedPassword.Length < 6)
            return ServiceResult<UserDto>.Fail("رمز عبور باید حداقل ۶ کاراکتر باشد.");
        if (!Enum.IsDefined(dto.Role))
            return ServiceResult<UserDto>.Fail("نقش کاربری نامعتبر است.");

        var trimmedUsername = dto.Username.Trim();
        var exists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Username == trimmedUsername);
        if (exists)
            return ServiceResult<UserDto>.Fail("نام کاربری تکراری است.");

        var user = new User
        {
            Username = trimmedUsername,
            FullName = dto.FullName.Trim(),
            Role = dto.Role,
            PasswordHash = PasswordHasher.Hash(normalizedPassword),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var roleName = user.Role == UserRole.Admin ? "مدیر" : "اپراتور";
        await _audit.LogAsync(AppConstants.AuditActions.UserCreated, "User", user.Id.ToString(),
            $"کاربر {user.Username} ({roleName}) ایجاد شد.");

        return ServiceResult<UserDto>.Ok(MapToDto(user), "کاربر جدید با موفقیت ایجاد شد.");
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync()
    {
        var users = await _context.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Id)
            .ToListAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<ServiceResult> SetUserActiveAsync(int userId, bool isActive)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user is null)
            return ServiceResult.Fail("کاربر یافت نشد.");

        user.IsActive = isActive;
        await _context.SaveChangesAsync();
        await _audit.LogAsync(AppConstants.AuditActions.UserUpdated, "User", user.Id.ToString(),
            $"وضعیت کاربر {user.Username} به {(isActive ? "فعال" : "غیرفعال")} تغییر کرد.");
        return ServiceResult.Ok(isActive ? "کاربر فعال شد." : "کاربر غیرفعال شد.");
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        FullName = user.FullName,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };
}
