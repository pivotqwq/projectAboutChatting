using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using UserManager.Domain.Entities;

namespace UserManager.Infrastracture.Configs
{
    public class UserConfig : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("T_Users");
            builder.OwnsOne(x => x.UserBasic, nb =>
            {
                nb.Property(b => b.PhoneNumber).HasMaxLength(20).IsUnicode(false);
                nb.Property(b => b.Email).HasMaxLength(100).IsUnicode(false);
            });
            builder.HasOne(x => x.UserAccessFail).WithOne(f => f.User)
                .HasForeignKey<UserAccessFail>(f => f.UserId);
            builder.Property("passwordHash").HasMaxLength(100).IsUnicode(false);
        }
    }
}
