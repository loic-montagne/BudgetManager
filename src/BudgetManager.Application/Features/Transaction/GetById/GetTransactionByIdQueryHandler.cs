using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;
using MediatR;

namespace BudgetManager.Application.Features.Transaction.GetById;

public sealed class GetTransactionByIdQueryHandler(ITransactionQueries queries, ICurrentUser currentUser) : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await queries.GetByIdAsync(request.Id, currentUser.RequiredUserId, Domain.Enums.Permission.View, cancellationToken);
        NotFoundException<TransactionDto>.ThrowIfNull(transaction, request.Id);
        return transaction!;
    }
}
