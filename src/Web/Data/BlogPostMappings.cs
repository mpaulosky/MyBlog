namespace MyBlog.Web.Data;

internal static class BlogPostMappings
{
	internal static BlogPostDto ToDto(this BlogPost post) => new(
			post.Id, post.Title, post.Content, post.Author,
			post.CreatedAt, post.UpdatedAt, post.IsPublished);
}
