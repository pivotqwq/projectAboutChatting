using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManager.Domain.Entities;

namespace UserManager.Infrastracture.Configs
{
    public class GroupMemberConfig : IEntityTypeConfiguration<GroupMember>
    {
        public void Configure(EntityTypeBuilder<GroupMember> builder)
        {
            builder.ToTable("T_GroupMembers");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
            builder.Property(x => x.Role).HasMaxLength(20).IsUnicode(false).IsRequired();
            builder.Property(x => x.Nickname).HasMaxLength(50).IsUnicode(true).IsRequired(false);
            builder.Property(x => x.JoinedAt).IsRequired();
        }
    }
}


