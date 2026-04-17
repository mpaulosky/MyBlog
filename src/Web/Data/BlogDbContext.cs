using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using MyBlog.Domain.Entities;

namespace MyBlog.Web.Data;

public sealed class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options)
{
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<BlogPost>();
        entity.ToCollection("blogposts");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Version).IsConcurrencyToken();
    }
}
