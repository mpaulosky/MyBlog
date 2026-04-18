namespace MyBlog.Web.Features.UserManagement;

public sealed record GetUsersWithRolesQuery : IRequest<Result<IReadOnlyList<UserWithRolesDto>>>;

public sealed record UserWithRolesDto(string UserId, string Email, string Name, IReadOnlyList<string> Roles);
