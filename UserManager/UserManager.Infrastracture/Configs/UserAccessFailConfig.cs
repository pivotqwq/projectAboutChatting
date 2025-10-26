using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManager.Domain.Entities;

namespace UserManager.Infrastracture.Configs
{
    public class UserAccessFailConfig : IEntityTypeConfiguration<UserAccessFail>
    {
        public void Configure(EntityTypeBuilder<UserAccessFail> builder)
        {
            builder.ToTable("T_UserAccessFails");
            builder.Property(f => f.AccessFailCount).IsRequired();
            builder.Property(f => f.LockEnd).IsRequired(false);
            builder.Property(f => f.IsLockedOut).IsRequired();
            builder.HasOne(f => f.User).WithOne(u => u.UserAccessFail)
                .HasForeignKey<UserAccessFail>(f => f.UserId);
        }
    }
}