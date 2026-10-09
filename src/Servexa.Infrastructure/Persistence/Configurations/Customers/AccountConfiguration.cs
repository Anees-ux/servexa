using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Customers;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", "customers");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(a => new { a.TenantId, a.Id });

        builder.Property(a => a.AccountNumber)
            .IsRequired()
            .HasMaxLength(50)
            .IsUnicode(false); // varchar(50)

        builder.HasIndex(a => new { a.TenantId, a.AccountNumber })
            .IsUnique();

        builder.Property(a => a.LegalName)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(a => a.DisplayName)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(a => a.AccountType)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(a => a.Status)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(a => a.DefaultBranchId)
            .IsRequired(false);

        builder.Property(a => a.PaymentTermsDays)
            .IsRequired(false);

        builder.Property(a => a.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsUnicode(false)
            .IsFixedLength(); // char(3)

        builder.Property(a => a.IsCreditHold)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.CreditHoldReason)
            .IsRequired(false)
            .HasMaxLength(500)
            .IsUnicode(true); // nvarchar(500)

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(a => a.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(a => a.Version)
            .IsRowVersion();

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional tenant-safe foreign key to Branch using composite alternate key (TenantId, Id) on Branches
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(a => new { a.TenantId, a.DefaultBranchId })
            .HasPrincipalKey(b => new { b.TenantId, b.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
