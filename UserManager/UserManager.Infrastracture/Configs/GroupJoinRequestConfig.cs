using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManager.Domain.Entities;

namespace UserManager.Infrastracture.Configs
{
    public class GroupJoinRequestConfig : IEntityTypeConfiguration<GroupJoinRequest>
    {
        public void Configure(EntityTypeBuilder<GroupJoinRequest> builder)
        {
            builder.ToTable("T_GroupJoinRequests");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.GroupId, x.RequesterId }).IsUnique();
            builder.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}

