using MediatR;
using Domain.Abstractions;

namespace MyBlog.Web.Features.UserManagement;

public sealed record AssignRoleCommand(string UserId, string RoleId) : IRequest<Result>;
