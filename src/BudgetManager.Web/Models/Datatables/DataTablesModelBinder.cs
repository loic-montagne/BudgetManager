using BudgetManager.Web.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BudgetManager.Web.Models.Datatables
{
    public class DataTablesModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext, nameof(bindingContext));

            var value = new DataTablesParameters()
            {
                Echo = bindingContext.ValueProvider.GetValue("sEcho").FirstValue,
                Search = bindingContext.ValueProvider.GetValue("sSearch").FirstValue,
                DisplayLength = bindingContext.ValueProvider.GetValue("iDisplayLength").FirstValue.ToInt(),
                DisplayStart = bindingContext.ValueProvider.GetValue("iDisplayStart").FirstValue.ToInt(),
                ColumnsCount = bindingContext.ValueProvider.GetValue("iColumns").FirstValue.ToInt(),
                SortingColsCount = bindingContext.ValueProvider.GetValue("iSortingCols").FirstValue.ToInt(),
                ColumnNames = bindingContext.ValueProvider.GetValue("sColumns").FirstValue,
            };

            var sorts = new List<DataTablesOrder>();
            for (int i = 0; i < value.SortingColsCount; i++)
            {
                sorts.Add(new DataTablesOrder()
                {
                    Column = bindingContext.ValueProvider.GetValue($"iSortCol_{i}").FirstValue.ToInt() - ((bindingContext.ValueProvider.GetValue("sColumns").FirstValue?.Contains("Responsive") ?? false) ? 1 : 0),
                    Dir = bindingContext.ValueProvider.GetValue($"sSortDir_{i}").FirstValue
                });
            }
            value.SortingCols = sorts.AsEnumerable();


            bindingContext.Result = ModelBindingResult.Success(value);

            return Task.CompletedTask;
        }
    }
}
