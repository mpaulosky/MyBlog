namespace MyBlog.Web.Features.BlogPosts.Edit;

public sealed record EditBlogPostCommand(Guid Id, string Title, string Content) : IRequest<Result>;
