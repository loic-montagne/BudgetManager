using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Web.Models.Datatables;

/// <summary>
/// Root model containing parameters sent by DataTables to an MVC Controller
/// </summary>
[ModelBinder(typeof(DataTablesModelBinder))]
public sealed record DataTablesParameters
{
    /// <summary>
    /// Request sequence number sent by DataTable,
    /// same value must be returned in response
    /// </summary>       
    public string? Echo { get; set; }

    /// <summary>
    /// Text used for filtering
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Number of records that should be shown in table
    /// </summary>
    public int DisplayLength { get; set; }

    /// <summary>
    /// First record that should be shown(used for paging)
    /// </summary>
    public int DisplayStart { get; set; }

    /// <summary>
    /// Number of columns in table
    /// </summary>
    public int ColumnsCount { get; set; }

    /// <summary>
    /// Number of columns that are used in sorting
    /// </summary>
    public int SortingColsCount { get; set; }

    /// <summary>
    /// Comma separated list of column names
    /// </summary>
    public string? ColumnNames { get; set; }

    /// <summary>
    /// Order no of the columns and sort directions that are used to do sorting
    /// </summary>
    public IEnumerable<DataTablesOrder>? SortingCols { get; set; }
}
