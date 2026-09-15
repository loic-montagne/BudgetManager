using MediatR;

namespace BudgetManager.Application.Abstractions.Messaging;

/// <summary>
/// Represents a read-only request that returns a result.
/// </summary>
/// <typeparam name="TResponse">The query result type.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;
