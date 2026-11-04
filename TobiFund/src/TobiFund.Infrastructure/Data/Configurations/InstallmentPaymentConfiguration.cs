using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class InstallmentPaymentConfiguration : IEntityTypeConfiguration<InstallmentPayment>
{
    public void Configure(EntityTypeBuilder<InstallmentPayment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.HasIndex(x => x.InstallmentId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Installment)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.InstallmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
