using TobiFund.Application.Common;
using TobiFund.Application.DTOs;

namespace TobiFund.Application.Interfaces;

public interface ISettingsService
{
    Task<IReadOnlyList<SettingDto>> GetAllAsync();
    Task<string> GetValueAsync(string key, string defaultValue = "");
    Task<int> GetIntAsync(string key, int defaultValue);
    Task<decimal> GetDecimalAsync(string key, decimal defaultValue);
    Task<ServiceResult> SetValueAsync(string key, string value);
    Task<ServiceResult> SaveAllAsync(IEnumerable<SettingDto> settings);
}
