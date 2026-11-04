using TobiFund.Application.DTOs;

namespace TobiFund.WPF.Services;

/// <summary>Reads an Excel file of members (MemberCode, FirstName, LastName, Capital, Loan).</summary>
public interface IExcelImportService
{
    IReadOnlyList<ImportMemberRowDto> ReadMembers(string filePath);
}
