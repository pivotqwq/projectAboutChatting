using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MatchingService.Domain.Entities;

namespace MatchingService.Infrastructure.Configurations
{
    /// <summary>
    /// 用户交互记录实体配置
    /// </summary>
    public class UserInteractionConfiguration : IEntityTypeConfiguration<UserInteraction>
    {
        public void Configure(EntityTypeBuilder<UserInteraction> builder)
        {
            builder.HasKey(ui => ui.Id);

            builder.Property(ui => ui.UserId)
                .IsRequired();

            builder.Property(ui => ui.TargetUserId)
                .IsRequired();

            builder.Property(ui => ui.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(ui => ui.Rating)
                .IsRequired()
                .HasDefaultValue(0.0f);

            builder.Property(ui => ui.CreatedAt)
                .IsRequired();

            builder.Property(ui => ui.Context)
                .HasMaxLength(1000);

            builder.Property(ui => ui.Metadata)
                .HasMaxLength(2000);

            // 创建索引
            builder.HasIndex(ui => ui.UserId);
            builder.HasIndex(ui => ui.TargetUserId);
            builder.HasIndex(ui => ui.Type);
            builder.HasIndex(ui => ui.CreatedAt);
            builder.HasIndex(ui => new { ui.UserId, ui.TargetUserId, ui.Type });
        }
    }
}
