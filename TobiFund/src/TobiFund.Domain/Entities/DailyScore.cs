namespace TobiFund.Domain.Entities;

/// <summary>
/// Snapshot روزانه امتیاز عضو.
/// ترکیب (MemberId + ScoreDate) یکتا است تا برای یک روز دو رکورد ثبت نشود.
/// این جدول تضمین می‌کند تغییر موجودی‌های آینده، امتیاز روزهای گذشته را تغییر ندهد.
/// </summary>
public class DailyScore
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    /// <summary>تاریخ snapshot (روز، midnight)</summary>
    public DateTime ScoreDate { get; set; }

    /// <summary>سرمایه عضو در آن روز</summary>
    public decimal CapitalAmount { get; set; }

    /// <summary>مانده وام عضو در آن روز</summary>
    public decimal LoanAmount { get; set; }

    /// <summary>امتیاز مثبت (بر اساس سرمایه)</summary>
    public int PositiveScore { get; set; }

    /// <summary>امتیاز منفی (بر اساس مانده وام)</summary>
    public int NegativeScore { get; set; }

    /// <summary>امتیاز خالص = مثبت - منفی</summary>
    public int NetScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public Member Member { get; set; } = null!;
}
