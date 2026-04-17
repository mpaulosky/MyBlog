using MediatR;
using MyBlog.Web.Data;
using Domain.Abstractions;

namespace MyBlog.Web.Features.BlogPosts.Edit;

public sealed record GetBlogPostByIdQuery(Guid Id) : IRequest<Result<BlogPostDto?>>;
