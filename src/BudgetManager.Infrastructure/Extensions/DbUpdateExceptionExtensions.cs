using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BudgetManager.Infrastructure.Extensions;

internal static class DbUpdateExceptionExtensions
{
    internal static string ToErrorString(this DbUpdateException e)
    {
        var sb = new StringBuilder();

        var innerException = e.InnerException;
        while (innerException != null)
        {
            sb.AppendLine(innerException.Message);
            innerException = innerException.InnerException;
        }

        if (e.Entries != null && e.Entries.Any())
        {
            foreach (var entry in e.Entries)
            {
                string keyString = "";
                var key = entry.Metadata.FindPrimaryKey();
                if (key != null)
                {
                    keyString = string.Join(", ",
                        key.Properties.Select(p =>
                        {
                            var property = entry.Property(p.Name);
                            var value = entry.State == EntityState.Deleted
                                ? property.OriginalValue
                                : property.CurrentValue;

                            return $"{p.Name}={value}";
                        }));
                }

                sb.AppendLine(
                    $"The entity of type \"{entry.Entity.GetType().Name}\" " +
                    $"in state \"{entry.State}\" " +
                    $"with key \"{keyString}\" cannot be saved.");
            }
        }

        return sb.ToString().TrimEnd();
    }
}
