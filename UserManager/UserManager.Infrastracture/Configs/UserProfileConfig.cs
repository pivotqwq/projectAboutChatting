using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManager.Domain.Entities;

namespace UserManager.Infrastracture.Configs
{
    public class UserProfileConfig : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.ToTable("T_UserProfiles");
            
            builder.HasKey(x => x.Id);
            
            // 配置与User的关系
            builder.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<UserProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 配置字段长度和约束
            builder.Property(x => x.Avatar)
                .HasMaxLength(500)
                .IsUnicode(false);
                
            builder.Property(x => x.RealName)
                .HasMaxLength(50)
                .IsUnicode(true);
                
            builder.Property(x => x.Gender)
                .HasMaxLength(10)
                .IsUnicode(false);
                
            builder.Property(x => x.Region)
                .HasMaxLength(100)
                .IsUnicode(true);
                
            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
                
            builder.Property(x => x.QRCodeImage)
                .HasMaxLength(500)
                .IsUnicode(false);
                
            builder.Property(x => x.Bio)
                .HasMaxLength(1000)
                .IsUnicode(true);
                
            builder.Property(x => x.CreatedAt)
                .IsRequired();
                
            builder.Property(x => x.UpdatedAt)
                .IsRequired();
        }
    }
}
