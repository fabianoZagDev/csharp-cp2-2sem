using System;
using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento relacional de <see cref="PaymentRecord"/>.
/// </summary>
public sealed class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PaymentRecords");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedNever();

        builder.Property(payment => payment.PaidById)
            .IsRequired()
            .HasMaxLength(ExpenseConfiguration.UserIdMaxLength);

        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.HasIndex(payment => payment.ExpenseId).IsUnique();
    }
}
