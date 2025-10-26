using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MatchingService.Domain.Entities;
using MatchingService.Domain.ValueObjects;

namespace MatchingService.Infrastructure.Configurations
{
    /// <summary>
    /// 用户匹配记录实体配置
    /// </summary>
    public class UserMatchConfiguration : IEntityTypeConfiguration<UserMatch>
    {
        public void Configure(EntityTypeBuilder<UserMatch> builder)
        {
            builder.HasKey(um => um.Id);

            builder.Property(um => um.UserId)
                .IsRequired();

            builder.Property(um => um.MatchedUserId)
                .IsRequired();

            builder.Property(um => um.MatchScore)
                .IsRequired()
                .HasDefaultValue(0.0f);

            builder.Property(um => um.MatchType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(um => um.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(MatchStatus.Pending);

            builder.Property(um => um.CreatedAt)
                .IsRequired();

            builder.Property(um => um.Notes)
                .HasMaxLength(500);

            // 创建索引
            builder.HasIndex(um => new { um.UserId, um.MatchedUserId }).IsUnique();
            builder.HasIndex(um => um.UserId);
            builder.HasIndex(um => um.MatchedUserId);
            builder.HasIndex(um => um.Status);
            builder.HasIndex(um => um.MatchType);
            builder.HasIndex(um => um.MatchScore);
        }
    }
}
