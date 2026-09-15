using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Extensions;
using BudgetManager.Infrastructure.Extensions;
using BudgetManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Queries;

internal sealed class BudgetAccessQueries(ApplicationDbContext context) : IBudgetAccessQueries
{
    public async Task<Application.Features.BudgetAccess.GetByKey.BudgetAccessDto?> GetByKeyAsync(Guid budgetId, Guid userId, Guid currentUserId, Permission requiredBudgetPermission, CancellationToken cancellationToken)
    {
        var data = await context
            .Set<BudgetAccess>()
            .AsNoTracking()
            .Include(x => x.Budget)
            .Where(x => x.BudgetId == budgetId
                     && x.UserId == userId)
            .Where(x => x.Budget.Accesses.Any(x => x.UserId == currentUserId
                                                && (x.IsOwner || (EF.Property<Permission>(x, BudgetAccess.PermissionsPropertyName) & requiredBudgetPermission) == requiredBudgetPermission)))
            .Join(
                context.Set<ApplicationUser>(),
                    x => x.UserId,
                    x => x.Id,
                    (ba, u) => new
                    {
                        BudgetAccess = ba,
                        User = u,
                        Permissions = EF.Property<Permission>(ba, BudgetAccess.PermissionsPropertyName)
                    })
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.BudgetAccess.CreatedBy,
                    x => x.Id,
                    (x, u) => new
                    {
                        x.BudgetAccess,
                        x.User,
                        x.Permissions,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.BudgetAccess,
                    x.User,
                    x.Permissions,
                    CreatedByUser = user
                })
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.BudgetAccess.UpdatedBy,
                    x => x.Id,
                    (x, u) => new
                    {
                        x.BudgetAccess,
                        x.User,
                        x.Permissions,
                        x.CreatedByUser,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.BudgetAccess,
                    x.User,
                    x.Permissions,
                    x.CreatedByUser,
                    UpdatedByUser = user
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
            return null;

        return new Application.Features.BudgetAccess.GetByKey.BudgetAccessDto(
            new Application.Features.BudgetAccess.GetByKey.BudgetDto(
                data.BudgetAccess.Budget.Id,
                data.BudgetAccess.Budget.Name),
            new Application.Features.User.Common.UserDto(
                data.User.Id,
                data.User.UserName ?? string.Empty,
                data.User.LastName,
                data.User.FirstName),
            data.BudgetAccess.IsOwner,
            data.BudgetAccess.IsOwner
                ? Permission.All.GetPermissions()
                : data.Permissions.GetPermissions(),
            data.BudgetAccess.CreatedBy,
            data.CreatedByUser.GetDisplayName(data.BudgetAccess.CreatedBy),
            data.BudgetAccess.CreatedOn,
            data.BudgetAccess.UpdatedBy,
            data.UpdatedByUser.GetDisplayName(data.BudgetAccess.UpdatedBy),
            data.BudgetAccess.UpdatedOn);
    }

    public async Task<IReadOnlyCollection<Application.Features.Budget.GetAccesses.BudgetAccessDto>> GetByBudgetIdAsync(Guid budgetId, Guid currentUserId, Permission requiredBudgetPermission, CancellationToken cancellationToken)
    {
        var data = await context
            .Set<BudgetAccess>()
            .AsNoTracking()
            .Include(x => x.Budget)
            .Where(x => x.BudgetId == budgetId)
            .Where(x => x.Budget.Accesses.Any(x => x.UserId == currentUserId
                                                && (x.IsOwner || (EF.Property<Permission>(x, BudgetAccess.PermissionsPropertyName) & requiredBudgetPermission) == requiredBudgetPermission)))
            .Join(
                context.Set<ApplicationUser>(),
                    x => x.UserId,
                    x => x.Id,
                    (ba, u) => new 
                    {
                        BudgetAccess = ba,
                        User = u,
                        Permissions = EF.Property<Permission>(ba, BudgetAccess.PermissionsPropertyName)
                    })
            .ToListAsync(cancellationToken);

        return data
            .Select(x => new Application.Features.Budget.GetAccesses.BudgetAccessDto(
                new Application.Features.Budget.GetAccesses.BudgetDto(
                    x.BudgetAccess.Budget.Id,
                    x.BudgetAccess.Budget.Name),
                new Application.Features.User.Common.UserDto(
                    x.User.Id,
                    x.User.UserName ?? string.Empty,
                    x.User.LastName,
                    x.User.FirstName),
                x.BudgetAccess.IsOwner,
                x.BudgetAccess.IsOwner
                    ? Permission.All.GetPermissions()
                    : x.Permissions.GetPermissions()))
            .ToList();
    }
}
