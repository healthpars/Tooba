using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Entities;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Services;

/// <summary>مدیریت اعضا (CRUD + حذف منطقی + جستجو)</summary>
public class MemberService : IMemberService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly IAppLogger _logger;

    public MemberService(
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

    public async Task<ServiceResult<MemberDto>> CreateAsync(CreateMemberDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName))
            return ServiceResult<MemberDto>.Fail("نام عضو الزامی است.");
        if (string.IsNullOrWhiteSpace(dto.LastName))
            return ServiceResult<MemberDto>.Fail("نام خانوادگی عضو الزامی است.");
        if (string.IsNullOrWhiteSpace(dto.MemberCode))
            return ServiceResult<MemberDto>.Fail("شناسه عضویت الزامی است.");

        if (await MemberCodeExistsAsync(dto.MemberCode))
            return ServiceResult<MemberDto>.Fail("شناسه عضویت تکراری است.");

        await using var transaction = await _context.BeginTransactionAsync();
        try
        {
            var member = new Member
            {
                MemberCode = dto.MemberCode.Trim(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Phone = dto.Phone?.Trim(),
                JoinDate = dto.JoinDate.Date,
                Status = MemberStatus.Active,
                Notes = dto.Notes?.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy = _currentUser.UserName,
                IsDeleted = false
            };

            _context.Members.Add(member);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(AppConstants.AuditActions.MemberCreated, "Member", member.Id.ToString(),
                $"عضو {member.FullName} با شناسه {member.MemberCode} ثبت شد.");
            await transaction.CommitAsync();

            var result = await GetByIdAsync(member.Id);
            return ServiceResult<MemberDto>.Ok(result ?? MapToDto(member), "عضو جدید با موفقیت ثبت شد.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error("خطا در ثبت عضو", ex);
            return ServiceResult<MemberDto>.Fail("خطا در ثبت عضو. لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(UpdateMemberDto dto)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == dto.Id && !m.IsDeleted);
        if (member is null)
            return ServiceResult.Fail("عضو یافت نشد.");

        if (string.IsNullOrWhiteSpace(dto.FirstName))
            return ServiceResult.Fail("نام عضو الزامی است.");
        if (string.IsNullOrWhiteSpace(dto.LastName))
            return ServiceResult.Fail("نام خانوادگی عضو الزامی است.");
        if (!Enum.IsDefined(dto.Status))
            return ServiceResult.Fail("وضعیت عضویت نامعتبر است.");

        member.FirstName = dto.FirstName.Trim();
        member.LastName = dto.LastName.Trim();
        member.Phone = dto.Phone?.Trim();
        member.Status = dto.Status;
        member.Notes = dto.Notes?.Trim();

        await _context.SaveChangesAsync();
        await _audit.LogAsync(AppConstants.AuditActions.MemberUpdated, "Member", member.Id.ToString(),
            $"اطلاعات عضو {member.FullName} ({member.MemberCode}) ویرایش شد.");
        return ServiceResult.Ok("اطلاعات عضو با موفقیت ویرایش شد.");
    }

    public async Task<ServiceResult> SoftDeleteAsync(int memberId)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId && !m.IsDeleted);
        if (member is null)
            return ServiceResult.Fail("عضو یافت نشد.");

        member.IsDeleted = true;
        await _context.SaveChangesAsync();
        await _audit.LogAsync(AppConstants.AuditActions.MemberDeleted, "Member", member.Id.ToString(),
            $"عضو {member.FullName} ({member.MemberCode}) حذف منطقی شد.");
        return ServiceResult.Ok("عضو حذف شد. (حذف منطقی)");
    }

    public async Task<ServiceResult> RestoreAsync(int memberId)
    {
        var member = await _context.Members
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == memberId && m.IsDeleted);
        if (member is null)
            return ServiceResult.Fail("عضو یافت نشد.");

        member.IsDeleted = false;
        await _context.SaveChangesAsync();
        return ServiceResult.Ok("عضو بازیابی شد.");
    }

    public async Task<MemberDto?> GetByIdAsync(int memberId)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId && !m.IsDeleted);
        if (member is null) return null;
        var dto = MapToDto(member);
        await FillComputedFieldsAsync(dto);
        return dto;
    }

    public async Task<IReadOnlyList<MemberDto>> GetAllAsync(string? search = null, bool includeDeleted = false)
    {
        IQueryable<Member> query = _context.Members;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                m.FirstName.Contains(term) ||
                m.LastName.Contains(term) ||
                m.MemberCode.Contains(term) ||
                (m.Phone != null && m.Phone.Contains(term)));
        }

        var members = await query.OrderBy(m => m.MemberCode).ToListAsync();
        var result = members.Select(MapToDto).ToList();
        foreach (var dto in result)
            await FillComputedFieldsAsync(dto);
        return result;
    }

    public async Task<MemberSummaryDto?> GetSummaryAsync(int memberId)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId && !m.IsDeleted);
        if (member is null) return null;
        return await SummaryCalculator.BuildSummaryAsync(_context, member);
    }

    public async Task<bool> MemberCodeExistsAsync(string memberCode, int? excludeMemberId = null)
    {
        var query = _context.Members.IgnoreQueryFilters().Where(m => m.MemberCode == memberCode.Trim());
        if (excludeMemberId.HasValue)
            query = query.Where(m => m.Id != excludeMemberId.Value);
        return await query.AnyAsync();
    }

    private async Task FillComputedFieldsAsync(MemberDto dto)
    {
        dto.TotalCapital = await SummaryCalculator.GetCapitalAsync(_context, dto.Id);
        dto.OutstandingLoan = await SummaryCalculator.GetOutstandingLoanAsync(_context, dto.Id);
        dto.DueInstallments = await SummaryCalculator.GetDueInstallmentsAsync(_context, dto.Id);
        dto.DueSubscription = await SummaryCalculator.GetDueSubscriptionAsync(_context, dto.Id);
        dto.TotalScore = await SummaryCalculator.GetTotalScoreAsync(_context, dto.Id);
        dto.Rank = await SummaryCalculator.GetRankAsync(_context, dto.Id);
    }

    private static MemberDto MapToDto(Member member) => new()
    {
        Id = member.Id,
        MemberCode = member.MemberCode,
        FirstName = member.FirstName,
        LastName = member.LastName,
        Phone = member.Phone,
        JoinDate = member.JoinDate,
        JoinDatePersian = Common.PersianDateHelper.ToPersianDate(member.JoinDate),
        Status = member.Status,
        Notes = member.Notes,
        IsDeleted = member.IsDeleted,
        CreatedAt = member.CreatedAt
    };
}
