using System;
using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento relacional de <see cref="ExpenseHistory"/>.
/// </summary>
public sealed class ExpenseHistoryConfiguration : IEntityTypeConfiguration<ExpenseHistory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpenseHistory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ExpenseHistory");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedOnAdd();

        builder.Property(history => history.Action)
            .HasConversion<string>()
            .HasMaxLength(ExpenseConfiguration.EnumMaxLength);

        builder.Property(history => history.ActorId)
            .IsRequired()
            .HasMaxLength(ExpenseConfiguration.UserIdMaxLength);

        builder.Property(history => history.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(ExpenseConfiguration.EnumMaxLength);

        builder.Property(history => history.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(ExpenseConfiguration.EnumMaxLength);

        builder.Property(history => history.Justification).HasMaxLength(Expense.RejectionReasonMaxLength);
        builder.Property(history => history.Changes).HasMaxLength(ExpenseHistory.ChangesMaxLength);

        builder.HasIndex(history => new { history.ExpenseId, history.OccurredAtUtc });
    }
}
