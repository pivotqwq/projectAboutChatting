namespace MatchingService.Domain.Entities
{
    /// <summary>
    /// 用户标签关系实体
    /// </summary>
    public record UserTag
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TagId { get; set; }
        public Tag Tag { get; set; }
        public float Weight { get; set; } // 权重，表示用户对该标签的兴趣程度
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public bool IsActive { get; set; }

        private UserTag() { }

        public UserTag(Guid userId, Guid tagId, float weight = 1.0f)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            TagId = tagId;
            Weight = weight;
            CreatedAt = DateTime.UtcNow;
            LastUpdatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        /// <summary>
        /// 更新权重
        /// </summary>
        public void UpdateWeight(float newWeight)
        {
            Weight = Math.Max(0.0f, Math.Min(1.0f, newWeight)); // 限制在0-1之间
            LastUpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 增加权重
        /// </summary>
        public void IncreaseWeight(float increment)
        {
            UpdateWeight(Weight + increment);
        }

        /// <summary>
        /// 减少权重
        /// </summary>
        public void DecreaseWeight(float decrement)
        {
            UpdateWeight(Weight - decrement);
        }

        /// <summary>
        /// 激活关系
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            LastUpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 停用关系
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            LastUpdatedAt = DateTime.UtcNow;
        }
    }
}
