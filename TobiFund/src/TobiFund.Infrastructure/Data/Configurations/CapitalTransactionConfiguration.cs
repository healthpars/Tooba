using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class CapitalTransactionConfiguration : IEntityTypeConfiguration<CapitalTransaction>
{
    public void Configure(EntityTypeBuilder<CapitalTransaction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).IsRequired();
        builder.Property(x => x.TransactionType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.HasIndex(x => x.MemberId);
        builder.HasIndex(x => x.TransactionDate);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Member)
            .WithMany(x => x.CapitalTransactions)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
