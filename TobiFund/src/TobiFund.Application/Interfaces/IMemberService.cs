using TobiFund.Application.Common;
using TobiFund.Application.DTOs;

namespace TobiFund.Application.Interfaces;

public interface IMemberService
{
    Task<ServiceResult<MemberDto>> CreateAsync(CreateMemberDto dto);
    Task<ServiceResult> UpdateAsync(UpdateMemberDto dto);
    Task<ServiceResult> SoftDeleteAsync(int memberId);
    Task<ServiceResult> RestoreAsync(int memberId);
    Task<MemberDto?> GetByIdAsync(int memberId);
    Task<IReadOnlyList<MemberDto>> GetAllAsync(string? search = null, bool includeDeleted = false);
    Task<MemberSummaryDto?> GetSummaryAsync(int memberId);
    Task<bool> MemberCodeExistsAsync(string memberCode, int? excludeMemberId = null);
}
