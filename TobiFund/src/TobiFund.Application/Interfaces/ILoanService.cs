using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Interfaces;

public interface ILoanService
{
    Task<ServiceResult<LoanDto>> CreateLoanAsync(CreateLoanDto dto);
    Task<IReadOnlyList<LoanDto>> GetLoansAsync(int? memberId = null);
    Task<IReadOnlyList<LoanInstallmentDto>> GetInstallmentsAsync(int? memberId = null, int? loanId = null);
    Task<IReadOnlyList<LoanInstallmentDto>> GetInstallmentsByStatusAsync(InstallmentStatus status);
    Task<ServiceResult<InstallmentPaymentDto>> PayInstallmentAsync(CreateInstallmentPaymentDto dto);
    Task<decimal> GetOutstandingLoanAsync(int memberId);
    Task<decimal> GetOutstandingLoanAtAsync(int memberId, DateTime date);
    Task<decimal> GetDueInstallmentsAsync(int memberId);

    /// <summary>به‌روزرسانی وضعیت اقساط و آبونمان‌ها بر اساس تاریخ امروز</summary>
    Task RefreshStatusesAsync();
}
