using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Username).IsUnique();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(100);
        builder.Property(x => x.Role).HasConversion<int>().IsRequired();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
