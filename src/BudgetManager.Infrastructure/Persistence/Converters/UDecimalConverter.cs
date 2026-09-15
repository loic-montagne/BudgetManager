using BudgetManager.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetManager.Infrastructure.Persistence.Converters;

internal sealed class UDecimalConverter()
    : ValueConverter<UDecimal, decimal>(
        value => value.ToDecimal(),
        value => new UDecimal(value))
{
}
