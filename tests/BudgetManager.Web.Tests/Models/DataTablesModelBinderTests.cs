using BudgetManager.Web.Models.Datatables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace BudgetManager.Web.Tests.Models;

public sealed class DataTablesModelBinderTests
{
    [Fact]
    public async Task BindModelAsync_WithModernProtocol_BindsAllValuesAndMultipleOrders()
    {
        var values = new Dictionary<string, StringValues>
        {
            ["draw"] = "9", ["search[value]"] = "needle", ["length"] = "25", ["start"] = "50",
            ["columns[0][name]"] = "Responsive",
            ["order[0][column]"] = "2", ["order[0][dir]"] = "asc",
            ["order[1][column]"] = "3", ["order[1][dir]"] = "desc"
        };

        var context = CreateContext(values);
        await new DataTablesModelBinder().BindModelAsync(context);

        var model = Assert.IsType<DataTablesParameters>(context.Result.Model);
        Assert.Equal(9, model.Draw);
        Assert.Equal("needle", model.Search);
        Assert.Equal(25, model.Length);
        Assert.Equal(50, model.Start);
        var orders = model.Orders.ToArray();
        Assert.Equal(1, orders[0].Column);
        Assert.Equal("asc", orders[0].Dir);
        Assert.Equal(2, orders[1].Column);
        Assert.Equal("desc", orders[1].Dir);
    }

    [Fact]
    public async Task BindModelAsync_WithMissingOrInvalidNumbers_UsesZero()
    {
        var context = CreateContext(new Dictionary<string, StringValues> { ["length"] = "invalid" });
        await new DataTablesModelBinder().BindModelAsync(context);
        var model = Assert.IsType<DataTablesParameters>(context.Result.Model);
        Assert.Equal(0, model.Draw);
        Assert.Equal(0, model.Length);
        Assert.Equal(0, model.Start);
        Assert.Empty(model.Orders);
    }

    [Fact]
    public async Task BindModelAsync_WithoutResponsiveColumn_DoesNotShiftOrderColumn()
    {
        var values = new Dictionary<string, StringValues>
        {
            ["columns[0][name]"] = "Name",
            ["order[0][column]"] = "1",
            ["order[0][dir]"] = "desc"
        };
        var context = CreateContext(values);

        await new DataTablesModelBinder().BindModelAsync(context);

        var model = Assert.IsType<DataTablesParameters>(context.Result.Model);
        var order = Assert.Single(model.Orders);
        Assert.Equal(1, order.Column);
        Assert.Equal("desc", order.Dir);
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
