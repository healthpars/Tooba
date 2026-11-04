namespace TobiFund.Application.DTOs;

/// <summary>یک ردیف از فایل اکسل اعضا: شناسه، نام، نام خانوادگی، سرمایه، وام</summary>
public class ImportMemberRowDto
{
    public string MemberCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public decimal Capital { get; set; }
    public decimal Loan { get; set; }
}

/// <summary>نتیجه واردات اکسل</summary>
public class ImportMembersResultDto
{
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}
