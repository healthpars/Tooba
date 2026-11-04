using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Entities;

namespace TobiFund.Application.Services;

/// <summary>مدیریت تنظیمات سیستم (Key-Value) — قابل تغییر توسط مدیر</summary>
public class SettingsService : ISettingsService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly IAppLogger _logger;

    public SettingsService(
        IApplicationDbContext context,
        IAuditService audit,
        ICurrentUserService currentUser,
        IAppLogger logger)
    {
        _context = context;
        _audit = audit;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SettingDto>> GetAllAsync()
    {
        await EnsureDefaultsAsync();
        var settings = await _context.Settings.OrderBy(s => s.Key).ToListAsync();
        return settings.Select(MapToDto).ToList();
    }

    public async Task<string> GetValueAsync(string key, string defaultValue = "")
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value ?? defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue)
    {
        var value = await GetValueAsync(key);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue)
    {
        var value = await GetValueAsync(key);
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    public async Task<ServiceResult> SetValueAsync(string key, string value)
    {
        if (!SettingKeys.Defaults.ContainsKey(key))
            return ServiceResult.Fail("کلید تنظیم نامعتبر است.");
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) || number < 0)
            return ServiceResult.Fail("مقدار تنظیم باید یک عدد غیرمنفی معتبر باشد.");

        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting is null)
        {
            setting = new Setting { Key = key };
            _context.Settings.Add(setting);
        }

        var oldValue = setting.Value;
        setting.Value = value.Trim();
        setting.Description = SettingKeys.Descriptions.GetValueOrDefault(key);
        setting.UpdatedAt = DateTime.Now;
        setting.UpdatedBy = _currentUser.UserName;

        await _context.SaveChangesAsync();
        await _audit.LogAsync(AppConstants.AuditActions.SettingsChanged, "Setting", key,
            $"تنظیم {key} از '{oldValue}' به '{setting.Value}' تغییر کرد.");
        return ServiceResult.Ok("تنظیم با موفقیت ذخیره شد.");
    }

    public async Task<ServiceResult> SaveAllAsync(IEnumerable<SettingDto> settings)
    {
        foreach (var dto in settings)
        {
            var result = await SetValueAsync(dto.Key, dto.Value);
            if (!result.Success)
                return result;
        }
        return ServiceResult.Ok("همه تنظیمات با موفقیت ذخیره شدند.");
    }

    /// <summary>ایجاد تنظیمات پیش‌فرض در صورت نبود (Seed)</summary>
    private async Task EnsureDefaultsAsync()
    {
        var existingKeys = await _context.Settings.Select(s => s.Key).ToListAsync();
        var missing = SettingKeys.Defaults.Where(kv => !existingKeys.Contains(kv.Key)).ToList();
        if (missing.Count == 0) return;

        foreach (var kv in missing)
        {
            _context.Settings.Add(new Setting
            {
                Key = kv.Key,
                Value = kv.Value,
                Description = SettingKeys.Descriptions.GetValueOrDefault(kv.Key),
                UpdatedAt = DateTime.Now,
                UpdatedBy = "System"
            });
        }
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("خطا در Seed تنظیمات", ex);
        }
    }

    private static SettingDto MapToDto(Setting s) => new()
    {
        Key = s.Key,
        Value = s.Value,
        Description = s.Description
    };
}
