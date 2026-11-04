using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.HasIndex(x => x.SubscriptionId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Subscription)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
