using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Service.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Service;

public sealed class WorkOrderCompletionEvaluationConfiguration : IEntityTypeConfiguration<WorkOrderCompletionEvaluation>
{
    public void Configure(EntityTypeBuilder<WorkOrderCompletionEvaluation> builder)
    {
        builder.ToTable("WorkOrderCompletionEvaluations", "service");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id });

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.WorkOrderId).IsRequired();
        builder.Property(x => x.CommandId).IsRequired();
        builder.Property(x => x.EvaluatedAtUtc).HasPrecision(3).IsRequired();
        builder.Property(x => x.EvaluatedByUserId);
        builder.Property(x => x.Outcome).HasColumnType("smallint").IsRequired();
        builder.Property(x => x.GateResultsJson).IsRequired();
        builder.Property(x => x.PolicySnapshotJson);
        builder.Property(x => x.TriggerBookingId);
        builder.Property(x => x.Summary).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        // Foreign key to WorkOrder
        builder.HasOne<WorkOrder>()
            .WithMany(w => w.CompletionEvaluations)
            .HasForeignKey(x => new { x.TenantId, x.WorkOrderId })
            .HasPrincipalKey(w => new { w.TenantId, w.Id })
            .OnDelete(DeleteBehavior.Cascade);

        // Physical model constraints & indexes
        builder.HasIndex(x => new { x.TenantId, x.CommandId })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.WorkOrderId, x.EvaluatedAtUtc });
    }
}
