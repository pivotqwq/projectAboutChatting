using MatchingService.Domain.ValueObjects;

namespace MatchingService.Domain.Entities
{
    /// <summary>
    /// 用户交互记录实体
    /// </summary>
    public record UserInteraction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TargetUserId { get; set; }
        public InteractionType Type { get; set; }
        public float Rating { get; set; } // 评分，用于协同过滤算法
        public DateTime CreatedAt { get; set; }
        public string? Context { get; set; } // 交互上下文信息
        public string? Metadata { get; set; } // 额外的元数据

        private UserInteraction() { }

        public UserInteraction(Guid userId, Guid targetUserId, InteractionType type, float rating = 0.0f)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            TargetUserId = targetUserId;
            Type = type;
            Rating = Math.Max(0.0f, Math.Min(5.0f, rating)); // 限制在0-5之间
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新评分
        /// </summary>
        public void UpdateRating(float newRating)
        {
            Rating = Math.Max(0.0f, Math.Min(5.0f, newRating));
        }

        /// <summary>
        /// 添加上下文信息
        /// </summary>
        public void AddContext(string context, string? metadata = null)
        {
            Context = context;
            Metadata = metadata;
        }
    }
}
