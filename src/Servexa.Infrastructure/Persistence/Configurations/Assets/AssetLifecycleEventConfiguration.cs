using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Assets.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Assets;

public sealed class AssetLifecycleEventConfiguration : IEntityTypeConfiguration<AssetLifecycleEvent>
{
    public void Configure(EntityTypeBuilder<AssetLifecycleEvent> builder)
    {
        builder.ToTable("AssetLifecycleEvents", "assets");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.AssetId).IsRequired();
        builder.Property(e => e.EventType).HasColumnType("smallint").IsRequired();
        builder.Property(e => e.PreviousStatus).HasColumnType("smallint");
        builder.Property(e => e.NewStatus).HasColumnType("smallint");
        builder.Property(e => e.FromSiteId);
        builder.Property(e => e.ToSiteId);
        builder.Property(e => e.FromOwnerAccountId);
        builder.Property(e => e.ToOwnerAccountId);
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.ActorUserId);
        builder.Property(e => e.OccurredAtUtc).HasPrecision(3).IsRequired();
        builder.Property(e => e.RecordedAtUtc).HasPrecision(3).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.AssetId, e.OccurredAtUtc });
    }
}
