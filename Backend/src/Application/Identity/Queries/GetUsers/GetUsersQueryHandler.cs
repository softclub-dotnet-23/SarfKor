using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Queries.GetUsers;

public sealed class GetUsersQueryHandler(
    IAuthService authService,
    IContributorTrustScoreRepository trustScoreRepository) : IQueryHandler<GetUsersQuery, GetUsersResult>
{
    public async Task<GetUsersResult> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var (users, totalCount) = await authService.SearchUsersAsync(query.Skip, query.Take, query.Search, cancellationToken);

        var dtos = new List<AdminUserListItemDto>();
        foreach (var user in users)
        {
            var trustScore = await trustScoreRepository.GetByUserIdAsync(user.UserId, cancellationToken);
            dtos.Add(new AdminUserListItemDto(user.UserId, user.Email, user.CreatedAt, user.IsBlocked, user.Roles, trustScore?.Score));
        }

        return new GetUsersResult(dtos, totalCount);
    }
}
