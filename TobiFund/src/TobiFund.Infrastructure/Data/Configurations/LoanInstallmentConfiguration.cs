using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class LoanInstallmentConfiguration : IEntityTypeConfiguration<LoanInstallment>
{
    public void Configure(EntityTypeBuilder<LoanInstallment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).IsRequired();
        builder.Property(x => x.PaidAmount).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(x => x.LoanId);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => new { x.LoanId, x.InstallmentNumber });
        builder.Ignore(x => x.RemainingAmount);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Loan)
            .WithMany(x => x.Installments)
            .HasForeignKey(x => x.LoanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Installment)
            .HasForeignKey(x => x.InstallmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
