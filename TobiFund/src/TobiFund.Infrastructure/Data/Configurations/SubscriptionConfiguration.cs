using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).IsRequired();
        builder.Property(x => x.PaidAmount).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.PersianYear).IsRequired();
        builder.Property(x => x.PersianMonth).IsRequired();
        // هر عضو در هر ماه از سال شمسی فقط یک رکورد آبونمان دارد
        builder.HasIndex(x => new { x.MemberId, x.PersianYear, x.PersianMonth }).IsUnique();
        builder.HasIndex(x => x.DueDate);
        builder.Ignore(x => x.RemainingAmount);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Member)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Subscription)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
