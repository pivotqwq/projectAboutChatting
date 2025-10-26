using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MatchingService.Domain.Entities;

namespace MatchingService.Infrastructure.Configurations
{
    /// <summary>
    /// 用户标签关系实体配置
    /// </summary>
    public class UserTagConfiguration : IEntityTypeConfiguration<UserTag>
    {
        public void Configure(EntityTypeBuilder<UserTag> builder)
        {
            builder.HasKey(ut => ut.Id);

            builder.Property(ut => ut.UserId)
                .IsRequired();

            builder.Property(ut => ut.TagId)
                .IsRequired();

            builder.Property(ut => ut.Weight)
                .IsRequired()
                .HasDefaultValue(1.0f);

            builder.Property(ut => ut.CreatedAt)
                .IsRequired();

            builder.Property(ut => ut.LastUpdatedAt)
                .IsRequired();

            builder.Property(ut => ut.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // 配置关系
            builder.HasOne(ut => ut.Tag)
                .WithMany()
                .HasForeignKey(ut => ut.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // 创建索引
            builder.HasIndex(ut => new { ut.UserId, ut.TagId }).IsUnique();
            builder.HasIndex(ut => ut.UserId);
            builder.HasIndex(ut => ut.TagId);
            builder.HasIndex(ut => ut.IsActive);
        }
    }
}
