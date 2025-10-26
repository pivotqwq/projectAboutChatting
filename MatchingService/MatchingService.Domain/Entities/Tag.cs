using MatchingService.Domain.ValueObjects;

namespace MatchingService.Domain.Entities
{
    /// <summary>
    /// 标签实体
    /// </summary>
    public record Tag : IAggregateRoot
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TagCategory Category { get; set; }
        public int UsageCount { get; private set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool IsActive { get; set; }

        private Tag() { }

        public Tag(string name, string description, TagCategory category)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description;
            Category = category;
            UsageCount = 0;
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        /// <summary>
        /// 增加使用次数
        /// </summary>
        public void IncrementUsage()
        {
            UsageCount++;
            LastUsedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 减少使用次数
        /// </summary>
        public void DecrementUsage()
        {
            if (UsageCount > 0)
            {
                UsageCount--;
            }
        }

        /// <summary>
        /// 激活标签
        /// </summary>
        public void Activate()
        {
            IsActive = true;
        }

        /// <summary>
        /// 停用标签
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// 更新标签信息
        /// </summary>
        public void UpdateInfo(string name, string description, TagCategory category)
        {
            Name = name;
            Description = description;
            Category = category;
        }
    }
}
