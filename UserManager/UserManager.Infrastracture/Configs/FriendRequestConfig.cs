using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManager.Domain.Entities;

namespace UserManager.Infrastracture.Configs
{
    public class FriendRequestConfig : IEntityTypeConfiguration<FriendRequest>
    {
        public void Configure(EntityTypeBuilder<FriendRequest> builder)
        {
            builder.ToTable("T_FriendRequests");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.RequesterId, x.ReceiverId }).IsUnique();
            builder.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}

