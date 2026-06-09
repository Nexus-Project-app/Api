using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Groups;

internal sealed class GroupJoinRequestConfiguration : IEntityTypeConfiguration<GroupJoinRequest>
{
    public void Configure(EntityTypeBuilder<GroupJoinRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).IsRequired();

        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
    }
}
