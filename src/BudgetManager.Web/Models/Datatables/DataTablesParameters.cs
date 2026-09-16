using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Web.Models.Datatables;

/// <summary>
/// Root model containing parameters sent by DataTables to an MVC Controller.
/// </summary>
[ModelBinder(typeof(DataTablesModelBinder))]
public sealed record DataTablesParameters
{
    /// <summary>Request sequence number. The same value must be returned in the response.</summary>
    public int Draw { get; set; }

    /// <summary>Global text used for filtering.</summary>
    public string? Search { get; set; }

    /// <summary>Number of records that should be shown in the table.</summary>
    public int Length { get; set; }

    /// <summary>Zero-based index of the first record that should be shown.</summary>
    public int Start { get; set; }

    /// <summary>Columns and directions used for ordering.</summary>
    public IEnumerable<DataTablesOrder> Orders { get; set; } = [];
}
