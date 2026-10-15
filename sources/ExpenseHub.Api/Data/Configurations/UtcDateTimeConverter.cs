using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Garante que instantes lidos do banco voltem marcados como UTC.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>
    /// Inicializa o conversor.
    /// </summary>
    public UtcDateTimeConverter()
        : base(
            value => value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
