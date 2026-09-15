using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Infrastructure.Extensions;
using BudgetManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BudgetManager.Infrastructure.Persistence.Queries;

internal sealed class UserQueries(ApplicationDbContext context, ILogger<UserQueries> logger) : IUserQueries
{
    public async Task<Application.Features.User.GetById.UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context
            .Set<ApplicationUser>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new 
            { 
                User = x,
                ProfilePhoto = context.Set<ApplicationUserProfilePicture>().FirstOrDefault(p => p.UserId == x.Id)
            })
            .Select(x => new Application.Features.User.GetById.UserDto(
                x.User.Id,
                x.User.UserName ?? string.Empty,
                x.User.LastName,
                x.User.FirstName,
                x.User.Email ?? string.Empty,
                x.User.EmailConfirmed,
                x.User.PreferredCulture,
                x.User.PreferredTheme,
                x.ProfilePhoto == null ? null : x.ProfilePhoto.Content,
                x.ProfilePhoto == null ? null : x.ProfilePhoto.ContentType))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Application.Features.User.GetById.CompleteUserDto?> GetCompleteByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var data = await context
            .Set<ApplicationUser>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                User = x,
                ProfilePhoto = context
                    .Set<ApplicationUserProfilePicture>()
                    .Where(p => p.UserId == x.Id)
                    .GroupJoin(
                        context.Set<ApplicationUser>(),
                            x => x.CreatedBy,
                            x => x.Id,
                            (p, u) => new
                            {
                                Photo = p,
                                Users = u
                            })
                    .SelectMany(
                        x => x.Users.DefaultIfEmpty(),
                        (x, user) => new
                        {
                            x.Photo,
                            CreatedByUser = user
                        })
                    .GroupJoin(
                        context.Set<ApplicationUser>(),
                            x => x.Photo.UpdatedBy,
                            x => x.Id,
                            (x, u) => new
                            {
                                x.Photo,
                                x.CreatedByUser,
                                Users = u
                            })
                    .SelectMany(
                        x => x.Users.DefaultIfEmpty(),
                        (x, user) => new
                        {
                            x.Photo,
                            x.CreatedByUser,
                            UpdatedByUser = user
                        })
                    .FirstOrDefault(),
                Roles = context
                    .Set<ApplicationUserRole>()
                    .Where(ur => ur.UserId == x.Id)
                    .Join(
                        context.Set<ApplicationRole>(),
                        x => x.RoleId,
                        x => x.Id,
                        (_, x) => x.Name ?? string.Empty)
                    .OrderBy(x => x)
                    .ToList(),
            })
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.User.CreatedBy,
                    x => x.Id,
                    (x, u) => new
                    {
                        x.User,
                        x.ProfilePhoto,
                        x.Roles,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.User,
                    x.ProfilePhoto,
                    x.Roles,
                    CreatedByUser = user
                })
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.User.UpdatedBy,
                    x => x.Id,
                    (x, u) => new
                    {
                        x.User,
                        x.ProfilePhoto,
                        x.Roles,
                        x.CreatedByUser,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.User,
                    x.ProfilePhoto,
                    x.Roles,
                    x.CreatedByUser,
                    UpdatedByUser = user
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
            return null;

        return new Application.Features.User.GetById.CompleteUserDto(
            data.User.Id,
            data.User.UserName ?? string.Empty,
            data.User.LastName,
            data.User.FirstName,
            data.User.Email ?? string.Empty,
            data.User.PhoneNumber,
            data.User.PreferredCulture,
            data.User.PreferredTheme,
            data.User.EmailConfirmed,
            data.User.ActivationEmailSentOn,
            data.User.ActivationEmailExpiresOn,
            data.Roles,
            data.ProfilePhoto?.Photo == null ? null : new Application.Features.User.GetById.ProfilePictureDto(
                data.ProfilePhoto.Photo.Content,
                data.ProfilePhoto.Photo.ContentType,
                data.ProfilePhoto.Photo.CreatedBy,
                data.ProfilePhoto.CreatedByUser.GetDisplayName(data.ProfilePhoto.Photo.CreatedBy),
                data.ProfilePhoto.Photo.CreatedOn,
                data.ProfilePhoto.Photo.UpdatedBy,
                data.ProfilePhoto.UpdatedByUser.GetDisplayName(data.ProfilePhoto.Photo.UpdatedBy),
                data.ProfilePhoto.Photo.UpdatedOn),
            data.User.CreatedBy,
            data.CreatedByUser.GetDisplayName(data.User.CreatedBy),
            data.User.CreatedOn,
            data.User.UpdatedBy,
            data.UpdatedByUser.GetDisplayName(data.User.UpdatedBy),
            data.User.UpdatedOn);
    }

    public async Task<Application.Features.User.GetCurrent.CurrentUserDto?> GetCurrentAsync(ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var id = currentUser.RequiredUserId;
        return await context
            .Set<ApplicationUser>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                User = x,
                ProfilePhoto = context.Set<ApplicationUserProfilePicture>()
                                      .Where(p => p.UserId == x.Id)
                                      .Select(p => new Application.Features.User.GetById.ProfilePictureDto(
                                          p.Content,
                                          p.ContentType,
                                          p.CreatedBy,
                                          string.Empty,
                                          p.CreatedOn,
                                          p.UpdatedBy,
                                          string.Empty,
                                          p.UpdatedOn))
                                      .FirstOrDefault()
            })
            .Select(x => new Application.Features.User.GetCurrent.CurrentUserDto(
                x.User.Id,
                x.User.FirstName,
                x.User.LastName,
                x.User.Email ?? string.Empty,
                x.User.PhoneNumber,
                context
                    .Set<ApplicationUserRole>()
                    .Where(ur => ur.UserId == x.User.Id)
                    .Join(
                        context.Set<ApplicationRole>(),
                        x => x.RoleId,
                        x => x.Id,
                        (_, x) => x.Name ?? string.Empty)
                    .OrderBy(x => x)
                    .ToList(),
                x.User.PreferredCulture,
                x.User.PreferredTheme,
                x.ProfilePhoto == null ? null : x.ProfilePhoto.Content,
                x.ProfilePhoto == null ? null : x.ProfilePhoto.ContentType))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<Application.Features.User.Search.UserDto>> SearchAsync(Application.Features.User.Search.PagedSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var searchCriteria = criteria.GetLikePatternSearchValues();
        Expression<Func<ApplicationUser, bool>> searchPredicate = _ => false;
        foreach (var search in searchCriteria)
        {
            var s = search;
            Expression<Func<ApplicationUser, bool>> currentPredicate =
                x =>
                    EF.Functions.Like(x.UserName, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter)
                 || EF.Functions.Like(x.LastName, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter)
                 || EF.Functions.Like(x.FirstName, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter)
                 || EF.Functions.Like(x.Email, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter)
                 || context.Set<ApplicationUserRole>()
                           .Where(ur => ur.UserId == x.Id)
                           .Join(
                               context.Set<ApplicationRole>(),
                               x => x.RoleId,
                               x => x.Id,
                               (_, x) => x.Name)
                           .Any(r =>EF.Functions.Like(r, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter));
            searchPredicate = searchPredicate.Or(currentPredicate);
        }


        return await context
            .Set<ApplicationUser>()
            .AsNoTracking()
            .GetPagedResultAsync(
                criteria,
                logger,
                x => new Application.Features.User.Search.UserDto(
                    x.Id,
                    x.Email ?? string.Empty,
                    x.LastName,
                    x.FirstName,
                    x.EmailConfirmed,
                    x.ActivationEmailSentOn,
                    x.ActivationEmailExpiresOn,
                    context
                        .Set<ApplicationUserRole>()
                        .Where(ur => ur.UserId == x.Id)
                        .Join(
                            context.Set<ApplicationRole>(),
                            x => x.RoleId,
                            x => x.Id,
                            (_, x) => x.Name ?? string.Empty)
                        .OrderBy(x => x)
                        .ToList()),
                x => criteria.IsActivated == null ||
                     criteria.IsActivated == x.EmailConfirmed,
                searchPredicate,
                x => x.Id,
                sort =>
                {
                    Expression<Func<ApplicationUser, object?>> keySelector =
                        sort.Field switch
                        {
                            UserSortField.LastName => x => x.LastName,
                            UserSortField.FirstName => x => x.FirstName,
                            UserSortField.Email => x => x.Email,
                            UserSortField.IsActivated => x => x.EmailConfirmed,
                            _ => x => x.UserName,
                        };
                    return keySelector;
                },
                cancellationToken);
    }
}
