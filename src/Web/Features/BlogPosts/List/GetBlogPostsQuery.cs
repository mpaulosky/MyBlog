namespace MyBlog.Web.Features.BlogPosts.List;

public sealed record GetBlogPostsQuery : IRequest<Result<IReadOnlyList<BlogPostDto>>>;
