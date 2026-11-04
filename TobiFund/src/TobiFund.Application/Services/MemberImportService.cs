using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Services;

/// <summary>
///Imports members from an Excel file (columns: MemberCode, FirstName, LastName, Capital, Loan).
///Each member is created; when Capital/Loan are present, an initial capital transaction and loan are recorded too.
///Validation: required fields, non-negative amounts, unique member code (within file and DB).
/// </summary>
public class MemberImportService : IMemberImportService
{
    private readonly IMemberService _memberService;
    private readonly ICapitalService _capitalService;
    private readonly ILoanService _loanService;
    private readonly ISettingsService _settings;
    private readonly IAuditService _audit;
    private readonly IAppLogger _logger;

    public MemberImportService(
        IMemberService memberService,
        ICapitalService capitalService,
        ILoanService loanService,
        ISettingsService settings,
        IAuditService audit,
        IAppLogger logger)
    {
        _memberService = memberService;
        _capitalService = capitalService;
        _loanService = loanService;
        _settings = settings;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ServiceResult<ImportMembersResultDto>> ImportMembersAsync(IReadOnlyList<ImportMemberRowDto> rows)
    {
        var result = new ImportMembersResultDto { TotalRows = rows.Count };
        if (rows.Count == 0)
            return ServiceResult<ImportMembersResultDto>.Ok(result, "هیچ ردیفی یافت نشد.");

        var defaultCount = await _settings.GetIntAsync(SettingKeys.DefaultInstallmentCount, 10);
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var code = (row.MemberCode ?? string.Empty).Trim();
            var first = (row.FirstName ?? string.Empty).Trim();
            var last = (row.LastName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last))
            {
                result.Skipped++;
                result.Errors.Add($"شناسه، نام یا نام خانوادگی خالی است. (شناسه: {code})");
                continue;
            }
            if (row.Capital < 0 || row.Loan < 0)
            {
                result.Skipped++;
                result.Errors.Add($"سرمایه یا وام منفی است. (شناسه: {code})");
                continue;
            }
            if (!seenCodes.Add(code))
            {
                result.Skipped++;
                result.Errors.Add($"شناسه تکراری در فایل: {code}");
                continue;
            }

            var createResult = await _memberService.CreateAsync(new CreateMemberDto
            {
                MemberCode = code,
                FirstName = first,
                LastName = last,
                JoinDate = DateTime.Today
            });
            if (!createResult.Success || createResult.Data is null)
            {
                result.Skipped++;
                result.Errors.Add($"{code}: {createResult.Message}");
                continue;
            }

            var memberId = createResult.Data.Id;

            if (row.Capital > 0)
            {
                var capResult = await _capitalService.AddTransactionAsync(new CreateCapitalTransactionDto
                {
                    MemberId = memberId,
                    TransactionDate = DateTime.Today,
                    Amount = row.Capital,
                    TransactionType = TransactionType.Deposit,
                    Description = "سرمایه اولیه (واردات از اکسل)"
                });
                if (!capResult.Success)
                    result.Errors.Add($"{code}: ثبت سرمایه ناموفق — {capResult.Message}");
            }

            if (row.Loan > 0)
            {
                var loanResult = await _loanService.CreateLoanAsync(new CreateLoanDto
                {
                    MemberId = memberId,
                    Amount = row.Loan,
                    LoanDate = DateTime.Today,
                    InstallmentCount = defaultCount,
                    Description = "وام اولیه (واردات از اکسل)"
                });
                if (!loanResult.Success)
                    result.Errors.Add($"{code}: ثبت وام ناموفق — {loanResult.Message}");
            }

            result.Imported++;
        }

        await _audit.LogAsync(AppConstants.AuditActions.MemberImported, "Member", null,
            $"واردات اکسل: {result.Imported} عضو ثبت شد، {result.Skipped} ردیف رد شد.");

        var message = $"واردات انجام شد: {result.Imported} عضو ثبت شد، {result.Skipped} ردیف رد شد.";
        if (result.Errors.Count > 0)
            message += $" ({result.Errors.Count} خطا)";
        return ServiceResult<ImportMembersResultDto>.Ok(result, message);
    }
}
