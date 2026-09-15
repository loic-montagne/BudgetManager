using BudgetManager.Application.Enums;

namespace BudgetManager.Application.Common.Pagination;

public sealed record SortCriterion<TField>(TField Field, SortDirection Direction) where TField : struct, Enum;
