using TobiFund.Application.Common;
using TobiFund.Application.DTOs;

namespace TobiFund.Application.Interfaces;

/// <summary>واردات اعضا از فایل اکسل (شناسه، نام، نام خانوادگی، سرمایه، وام)</summary>
public interface IMemberImportService
{
    Task<ServiceResult<ImportMembersResultDto>> ImportMembersAsync(IReadOnlyList<ImportMemberRowDto> rows);
}
