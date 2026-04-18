namespace MyBlog.Web.Features.BlogPosts.Create;

public sealed record CreateBlogPostCommand(string Title, string Content, string Author)
		: IRequest<Result<Guid>>;
