using System;
using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento relacional de <see cref="Expense"/>.
/// </summary>
public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    /// <summary>
    /// Tamanho das colunas que guardam identificadores de usuário do Identity.
    /// </summary>
    public const int UserIdMaxLength = 450;

    /// <summary>
    /// Tamanho das colunas que guardam enums como texto.
    /// </summary>
    public const int EnumMaxLength = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Expenses");
        builder.HasKey(expense => expense.Id);
        builder.Property(expense => expense.Id).ValueGeneratedNever();

        builder.Property(expense => expense.OwnerId)
            .IsRequired()
            .HasMaxLength(UserIdMaxLength);

        builder.Property(expense => expense.Description)
            .IsRequired()
            .HasMaxLength(Expense.DescriptionMaxLength);

        builder.Property(expense => expense.Amount).HasPrecision(18, 2);

        builder.Property(expense => expense.Status)
            .HasConversion<string>()
            .HasMaxLength(EnumMaxLength);

        builder.Property(expense => expense.RejectionReason).HasMaxLength(Expense.RejectionReasonMaxLength);
        builder.Property(expense => expense.Version).IsConcurrencyToken();

        builder.HasOne(expense => expense.Category)
            .WithMany()
            .HasForeignKey(expense => expense.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(expense => expense.History)
            .WithOne()
            .HasForeignKey(history => history.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(expense => expense.History).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(expense => expense.Payment)
            .WithOne()
            .HasForeignKey<PaymentRecord>(payment => payment.ExpenseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(expense => expense.OwnerId);
        builder.HasIndex(expense => expense.Status);
    }
}
