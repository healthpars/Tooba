using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Value).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.UpdatedBy).HasMaxLength(50);
    }
}
