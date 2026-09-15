using BudgetManager.Web.Models.Datatables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace BudgetManager.Web.Tests.Models;

public sealed class DataTablesModelBinderTests
{
    [Fact]
    public async Task BindModelAsync_WithLegacyProtocol_BindsAllValuesAndMultipleSorts()
    {
        var values = new Dictionary<string, StringValues>
        {
            ["sEcho"] = "9", ["sSearch"] = "needle", ["iDisplayLength"] = "25", ["iDisplayStart"] = "50",
            ["iColumns"] = "5", ["iSortingCols"] = "2", ["sColumns"] = "Responsive,Name,Bic",
            ["iSortCol_0"] = "2", ["sSortDir_0"] = "asc", ["iSortCol_1"] = "3", ["sSortDir_1"] = "desc"
        };
        var context = CreateContext(values);
        await new DataTablesModelBinder().BindModelAsync(context);
        var model = Assert.IsType<DataTablesParameters>(context.Result.Model);
        Assert.Equal("9", model.Echo);
        Assert.Equal("needle", model.Search);
        Assert.Equal(25, model.DisplayLength);
        Assert.Equal(50, model.DisplayStart);
        Assert.Equal(5, model.ColumnsCount);
        Assert.Equal(2, model.SortingColsCount);
        Assert.Equal("Responsive,Name,Bic", model.ColumnNames);
        var sorts = model.SortingCols!.ToArray();
        Assert.Equal(1, sorts[0].Column);
        Assert.Equal("asc", sorts[0].Dir);
        Assert.Equal(2, sorts[1].Column);
        Assert.Equal("desc", sorts[1].Dir);
    }

    [Fact]
    public async Task BindModelAsync_WithMissingOrInvalidNumbers_UsesZero()
    {
        var context = CreateContext(new Dictionary<string, StringValues> { ["iDisplayLength"] = "invalid" });
        await new DataTablesModelBinder().BindModelAsync(context);
        var model = Assert.IsType<DataTablesParameters>(context.Result.Model);
        Assert.Equal(0, model.DisplayLength);
        Assert.Equal(0, model.DisplayStart);
        Assert.Empty(model.SortingCols!);
    }

    [Fact]
    public async Task BindModelAsync_WithNullContext_Throws()
        => await Assert.ThrowsAsync<ArgumentNullException>(() => new DataTablesModelBinder().BindModelAsync(null!));

    private static DefaultModelBindingContext CreateContext(Dictionary<string, StringValues> values)
    {
        var http = new DefaultHttpContext();
        http.Request.QueryString = QueryString.Create(values.SelectMany(x => x.Value.Select(v => new KeyValuePair<string, string?>(x.Key, v))));
        var metadataProvider = new EmptyModelMetadataProvider();
        return new DefaultModelBindingContext
        {
            ModelMetadata = metadataProvider.GetMetadataForType(typeof(DataTablesParameters)),
            ModelName = "param",
            ModelState = new ModelStateDictionary(),
            ValueProvider = new QueryStringValueProvider(BindingSource.Query, http.Request.Query, System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
