using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).IsRequired();
        builder.Property(x => x.InstallmentAmount).IsRequired();
        builder.Property(x => x.InstallmentCount).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.HasIndex(x => x.MemberId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Member)
            .WithMany(x => x.Loans)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Installments)
            .WithOne(x => x.Loan)
            .HasForeignKey(x => x.LoanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
