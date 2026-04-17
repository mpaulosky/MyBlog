using MediatR;
using MyBlog.Domain.Common;

namespace MyBlog.Web.Features.BlogPosts.Delete;

public sealed record DeleteBlogPostCommand(Guid Id) : IRequest<Result>;
