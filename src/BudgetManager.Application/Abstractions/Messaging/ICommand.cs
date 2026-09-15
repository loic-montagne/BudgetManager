using MediatR;

namespace BudgetManager.Application.Abstractions.Messaging;

/// <summary>
/// Represents a command that does not return a result.
/// </summary>
public interface ICommand : IRequest;

/// <summary>
/// Represents a command that returns a result.
/// </summary>
/// <typeparam name="TResponse">The command result type.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;
