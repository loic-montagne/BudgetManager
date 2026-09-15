using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;
using MediatR;

namespace BudgetManager.Application.Features.User.GetById;

public sealed class GetUserByIdQueryHandler(IUserQueries queries) : IRequestHandler<GetUserByIdQuery, CompleteUserDto>
{
    public async Task<CompleteUserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await queries.GetCompleteByIdAsync(request.Id, cancellationToken);
        NotFoundException<CompleteUserDto>.ThrowIfNull(user, request.Id);
        return user!;
    }
}
