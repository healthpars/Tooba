using TobiFund.Application.DTOs;
using TobiFund.Domain.Enums;

namespace TobiFund.Tests;

/// <summary>تست‌های مدیریت اعضا (بخش ۷ سند)</summary>
public class MemberServiceTests
{
    [Fact]
    public async Task CreateMember_AndDuplicateCode_Rejected()
    {
        using var ts = new TestServices();
        var first = await ts.Members.CreateAsync(new CreateMemberDto
        {
            MemberCode = "1001", FirstName = "علی", LastName = "رضایی"
        });
        Assert.True(first.Success);

        // شناسه تکراری ثبت نشود
        var duplicate = await ts.Members.CreateAsync(new CreateMemberDto
        {
            MemberCode = "1001", FirstName = "مریم", LastName = "احمدی"
        });
        Assert.False(duplicate.Success);
    }

    [Fact]
    public async Task SoftDelete_ThenSearch_DoesNotReturnDeleted()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync("1001", "علی", "رضایی");

        var delete = await ts.Members.SoftDeleteAsync(memberId);
        Assert.True(delete.Success);

        var all = await ts.Members.GetAllAsync();
        Assert.Empty(all);

        var withDeleted = await ts.Members.GetAllAsync(includeDeleted: true);
        Assert.Single(withDeleted);
        Assert.True(withDeleted[0].IsDeleted);
    }

    [Fact]
    public async Task Search_ByNameCodePhone_Works()
    {
        using var ts = new TestServices();
        await ts.CreateMemberAsync("1001", "علی", "رضایی");
        await ts.CreateMemberAsync("1002", "رضا", "محمدی");

        Assert.Single(await ts.Members.GetAllAsync("علی"));
        Assert.Single(await ts.Members.GetAllAsync("1002"));
        Assert.Equal(2, (await ts.Members.GetAllAsync("ا")).Count); // نام/نام خانوادگی با «ا»
    }

    [Fact]
    public async Task UpdateMember_ChangesFields()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync("1001", "علی", "رضایی");

        var update = await ts.Members.UpdateAsync(new UpdateMemberDto
        {
            Id = memberId,
            FirstName = "علی‌اکبر",
            LastName = "رضایی",
            Phone = "09120000000",
            Status = MemberStatus.Inactive,
            Notes = "یادداشت تست"
        });
        Assert.True(update.Success);

        var member = await ts.Members.GetByIdAsync(memberId);
        Assert.Equal("علی‌اکبر", member!.FirstName);
        Assert.Equal(MemberStatus.Inactive, member.Status);
    }
}
