using MediatR;
using MyBlog.Domain.Common;

namespace MyBlog.Web.Features.UserManagement;

public sealed record GetAvailableRolesQuery : IRequest<Result<IReadOnlyList<RoleDto>>>;

public sealed record RoleDto(string Id, string Name);
