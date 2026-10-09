using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Service.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Service;

public sealed class WorkOrderStatusHistoryConfiguration : IEntityTypeConfiguration<WorkOrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<WorkOrderStatusHistory> builder)
    {
        builder.ToTable("WorkOrderStatusHistory", "service");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.TenantId).IsRequired();
        builder.Property(h => h.WorkOrderId).IsRequired();
        builder.Property(h => h.FromStatus).HasColumnType("smallint").IsRequired();
        builder.Property(h => h.ToStatus).HasColumnType("smallint").IsRequired();
        builder.Property(h => h.PauseReasonCode).HasMaxLength(50).IsUnicode(false);
        builder.Property(h => h.ChangedByUserId);
        builder.Property(h => h.ChangedAtUtc).HasPrecision(3).IsRequired();
        builder.Property(h => h.Reason).HasMaxLength(500);

        builder.HasIndex(h => new { h.TenantId, h.WorkOrderId, h.ChangedAtUtc });
    }
}
