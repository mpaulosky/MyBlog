using MediatR;
using MyBlog.Domain.Common;

namespace MyBlog.Web.Features.UserManagement;

public sealed record AssignRoleCommand(string UserId, string RoleId) : IRequest<Result>;
