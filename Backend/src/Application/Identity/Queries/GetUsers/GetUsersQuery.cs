namespace Application.Identity.Queries.GetUsers;

public sealed record GetUsersQuery(int Skip, int Take, string? Search);
