using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManager.Domain.Entities;

namespace UserManager.Infrastracture.Configs
{
    public class UserLoginHistoryConfig : IEntityTypeConfiguration<UserLoginHistory>
    {
        public void Configure(EntityTypeBuilder<UserLoginHistory> builder)
        {
            builder.ToTable("T_UserLoginHistories");
            builder.OwnsOne(x => x.UserBasic, nb =>
            {
                nb.Property(b => b.PhoneNumber).HasMaxLength(20).IsUnicode(false);
                nb.Property(b => b.Email).HasMaxLength(100).IsUnicode(false);
            });
        }
    }
}
