using TobiFund.Application.DTOs;
using TobiFund.Application.Services;

namespace TobiFund.Tests;

/// <summary>تست‌های سرویس واردات اکسل اعضا</summary>
public class MemberImportServiceTests
{
    private static MemberImportService CreateImportService(TestServices ts) =>
        new(ts.Members, ts.Capital, ts.Loans, ts.Settings, ts.Audit, ts.Logger);

    [Fact]
    public async Task Import_CreatesMembers_WithCapitalAndLoan()
    {
        using var ts = new TestServices();
        var import = CreateImportService(ts);

        var rows = new List<ImportMemberRowDto>
        {
            new() { MemberCode = "A1", FirstName = "علی", LastName = "رضایی", Capital = 500000m, Loan = 200000m },
            new() { MemberCode = "A2", FirstName = "مریم", LastName = "احمدی", Capital = 0m, Loan = 0m }
        };

        var result = await import.ImportMembersAsync(rows);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Imported);
        Assert.Equal(0, result.Data.Skipped);

        var all = await ts.Members.GetAllAsync();
        var a1 = all.First(m => m.MemberCode == "A1");
        Assert.Equal(500000m, a1.TotalCapital);
        Assert.Equal(200000m, a1.OutstandingLoan);
    }

    [Fact]
    public async Task Import_SkipsDuplicateCode_WithinFile()
    {
        using var ts = new TestServices();
        var import = CreateImportService(ts);
        var rows = new List<ImportMemberRowDto>
        {
            new() { MemberCode = "DUP", FirstName = "الف", LastName = "ب" },
            new() { MemberCode = "DUP", FirstName = "ج", LastName = "د" }
        };

        var result = await import.ImportMembersAsync(rows);

        Assert.Equal(1, result.Data!.Imported);
        Assert.Equal(1, result.Data.Skipped);
        Assert.Contains(result.Data.Errors, e => e.Contains("تکراری"));
    }

    [Fact]
    public async Task Import_SkipsEmptyRequiredFields()
    {
        using var ts = new TestServices();
        var import = CreateImportService(ts);
        var rows = new List<ImportMemberRowDto>
        {
            new() { MemberCode = "", FirstName = "", LastName = "" },
            new() { MemberCode = "OK1", FirstName = "نام", LastName = "فامیل" }
        };

        var result = await import.ImportMembersAsync(rows);

        Assert.Equal(1, result.Data!.Imported);
        Assert.Equal(1, result.Data.Skipped);
    }

    [Fact]
    public async Task Import_RejectsNegativeAmounts()
    {
        using var ts = new TestServices();
        var import = CreateImportService(ts);
        var rows = new List<ImportMemberRowDto>
        {
            new() { MemberCode = "NEG", FirstName = "الف", LastName = "ب", Capital = -100m }
        };

        var result = await import.ImportMembersAsync(rows);

        Assert.Equal(0, result.Data!.Imported);
        Assert.Equal(1, result.Data.Skipped);
    }

    [Fact]
    public async Task Import_SkipsDuplicateCode_AlreadyInDatabase()
    {
        using var ts = new TestServices();
        await ts.CreateMemberAsync("EXIST", "موجود", "کاربر");
        var import = CreateImportService(ts);
        var rows = new List<ImportMemberRowDto>
        {
            new() { MemberCode = "EXIST", FirstName = "جدید", LastName = "کاربر" }
        };

        var result = await import.ImportMembersAsync(rows);

        Assert.Equal(0, result.Data!.Imported);
        Assert.Equal(1, result.Data.Skipped);
    }
}
