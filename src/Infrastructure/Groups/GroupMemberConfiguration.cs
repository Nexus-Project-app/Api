using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Groups;

internal sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.GroupId, m.UserId }).IsUnique();

        builder.Property(m => m.Role).IsRequired();

        builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId);
    }
}
