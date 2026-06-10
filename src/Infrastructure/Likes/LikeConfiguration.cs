using Domain.Likes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Likes;

internal sealed class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.Post)
            .WithMany()
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Author)
            .WithMany()
            .HasForeignKey(l => l.AuthorId);

        // Un utilisateur ne peut liker qu'une seule fois par post
        builder.HasIndex(l => new { l.PostId, l.AuthorId }).IsUnique();
    }
}
