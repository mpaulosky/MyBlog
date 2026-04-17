namespace MyBlog.Web.Data;

public sealed record BlogPostDto(
    Guid Id,
    string Title,
    string Content,
    string Author,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsPublished);
