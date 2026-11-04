using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Common;
using TobiFund.Domain.Entities;
using TobiFund.Domain.Enums;

namespace TobiFund.Infrastructure.Data;

/// <summary>
/// آماده‌سازی اولیه دیتابیس:
/// 1. اجرای Migrations
/// 2. Seed تنظیمات پیش‌فرض
/// 3. ایجاد کاربر admin (رمز: 123456789 — هش‌شده)
/// </summary>
public static class DbInitializer
{
    public static void Initialize(TobiFundDbContext context)
    {
        // 1. Migration
        context.Database.Migrate();

        // 2. Seed Settings
        SeedSettings(context);

        // 3. Seed Admin User
        SeedAdminUser(context);
    }

    private static void SeedSettings(TobiFundDbContext context)
    {
        var existingKeys = context.Settings.Select(s => s.Key).ToList();
        var added = false;

        foreach (var kv in SettingKeys.Defaults)
        {
            if (existingKeys.Contains(kv.Key)) continue;
            context.Settings.Add(new Setting
            {
                Key = kv.Key,
                Value = kv.Value,
                Description = SettingKeys.Descriptions.GetValueOrDefault(kv.Key),
                UpdatedAt = DateTime.Now,
                UpdatedBy = "System"
            });
            added = true;
        }

        if (added)
            context.SaveChanges();
    }

    private static void SeedAdminUser(TobiFundDbContext context)
    {
        var exists = context.Users.IgnoreQueryFilters()
            .Any(u => u.Username == AppConstants.DefaultAdminUsername);
        if (exists) return;

        context.Users.Add(new User
        {
            Username = AppConstants.DefaultAdminUsername,
            // رمز عبور به‌صورت Hash ذخیره می‌شود — هرگز Plain Text
            PasswordHash = PasswordHasher.Hash(AppConstants.DefaultAdminPassword),
            FullName = "مدیر سیستم",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        context.SaveChanges();
    }
}
