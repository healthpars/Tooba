using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data.Configurations;

public class DailyScoreConfiguration : IEntityTypeConfiguration<DailyScore>
{
    public void Configure(EntityTypeBuilder<DailyScore> builder)
    {
        builder.HasKey(x => x.Id);
        // برای هر عضو در هر روز فقط یک Snapshot ثبت می‌شود
        builder.HasIndex(x => new { x.MemberId, x.ScoreDate }).IsUnique();
        builder.HasIndex(x => x.ScoreDate);

        builder.HasOne(x => x.Member)
            .WithMany(x => x.DailyScores)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
