using MediatR;
using Domain.Abstractions;

namespace MyBlog.Web.Features.UserManagement;

public sealed record RemoveRoleCommand(string UserId, string RoleId) : IRequest<Result>;
