using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MatchingService.Domain.Entities;

namespace MatchingService.Infrastructure.Configurations
{
    /// <summary>
    /// 标签实体配置
    /// </summary>
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.Description)
                .HasMaxLength(200);

            builder.Property(t => t.Category)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(t => t.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            builder.Property(t => t.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // 创建索引
            builder.HasIndex(t => t.Name).IsUnique();
            builder.HasIndex(t => t.Category);
            builder.HasIndex(t => t.UsageCount);
            builder.HasIndex(t => t.IsActive);
        }
    }
}
