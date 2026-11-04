using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MemberCode).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.MemberCode).IsUnique();
        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(50);
        builder.Property(x => x.LastName).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Ignore(x => x.FullName);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasMany(x => x.CapitalTransactions)
            .WithOne(x => x.Member)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Loans)
            .WithOne(x => x.Member)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Subscriptions)
            .WithOne(x => x.Member)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.DailyScores)
            .WithOne(x => x.Member)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
