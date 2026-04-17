using MediatR;
using MyBlog.Web.Data;
using MyBlog.Domain.Common;

namespace MyBlog.Web.Features.BlogPosts.List;

public sealed record GetBlogPostsQuery : IRequest<Result<IReadOnlyList<BlogPostDto>>>;
