using BudgetManager.Web.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BudgetManager.Web.Models.Datatables;

public class DataTablesModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext, nameof(bindingContext));

        var value = new DataTablesParameters
        {
            Draw = bindingContext.ValueProvider.GetValue("draw").FirstValue.ToInt(),
            Search = bindingContext.ValueProvider.GetValue("search[value]").FirstValue,
            Length = bindingContext.ValueProvider.GetValue("length").FirstValue.ToInt(),
            Start = bindingContext.ValueProvider.GetValue("start").FirstValue.ToInt()
        };

        var orders = new List<DataTablesOrder>();
        for (var i = 0; ; i++)
        {
            var columnValue = bindingContext.ValueProvider.GetValue($"order[{i}][column]");
            if (columnValue == ValueProviderResult.None)
                break;

            var column = columnValue.FirstValue.ToInt();
            var hasResponsiveColumn = string.Equals(
                bindingContext.ValueProvider.GetValue("columns[0][name]").FirstValue,
                "Responsive",
                StringComparison.OrdinalIgnoreCase);

            orders.Add(new DataTablesOrder
            {
                Column = column - (hasResponsiveColumn ? 1 : 0),
                Dir = bindingContext.ValueProvider.GetValue($"order[{i}][dir]").FirstValue
            });
        }

        value.Orders = orders;
        bindingContext.Result = ModelBindingResult.Success(value);
        return Task.CompletedTask;
    }
}
