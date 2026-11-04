using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(50);
        builder.Property(x => x.EntityType).HasMaxLength(50);
        builder.Property(x => x.EntityId).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Action);
    }
}
