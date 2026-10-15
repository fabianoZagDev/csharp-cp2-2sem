using System;
using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento relacional de <see cref="ExpenseCategory"/> e categorias iniciais.
/// </summary>
public sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ExpenseCategories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(ExpenseCategory.NameMaxLength);

        builder.HasIndex(category => category.Name).IsUnique();

        builder.HasData(
            new { Id = 1, Name = "Alimentação" },
            new { Id = 2, Name = "Transporte" },
            new { Id = 3, Name = "Hospedagem" },
            new { Id = 4, Name = "Material de escritório" },
            new { Id = 5, Name = "Outros" });
    }
}
