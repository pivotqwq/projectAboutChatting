using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManager.Domain.Entities;

namespace UserManager.Infrastracture.Configs
{
    public class FriendshipConfig : IEntityTypeConfiguration<Friendship>
    {
        public void Configure(EntityTypeBuilder<Friendship> builder)
        {
            builder.ToTable("T_Friendships");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.UserId, x.FriendUserId }).IsUnique();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}


