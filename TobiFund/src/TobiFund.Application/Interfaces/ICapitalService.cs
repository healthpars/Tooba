using TobiFund.Application.Common;
using TobiFund.Application.DTOs;

namespace TobiFund.Application.Interfaces;

public interface ICapitalService
{
    Task<ServiceResult<CapitalTransactionDto>> AddTransactionAsync(CreateCapitalTransactionDto dto);
    Task<IReadOnlyList<CapitalTransactionDto>> GetMemberTransactionsAsync(int memberId);
    Task<IReadOnlyList<CapitalTransactionDto>> GetAllAsync(DateTime? from = null, DateTime? to = null);
    Task<decimal> GetBalanceAsync(int memberId);
    Task<decimal> GetBalanceAtAsync(int memberId, DateTime date);
}
