using Domain.Comment;
using Domain.Posts;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Comments;

internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).IsRequired().HasMaxLength(2000);

        builder.HasOne(c => c.Post).WithMany(p => p.Comments).HasForeignKey(c => c.PostId);
        builder.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId);
    }
}
