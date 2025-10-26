using MatchingService.Domain.ValueObjects;
using MatchType = MatchingService.Domain.ValueObjects.MatchType;

namespace MatchingService.Domain.Entities
{
    /// <summary>
    /// 用户匹配记录实体
    /// </summary>
    public record UserMatch
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid MatchedUserId { get; set; }
        public float MatchScore { get; set; } // 匹配分数 0-1
        public MatchType MatchType { get; set; } // 匹配类型
        public DateTime CreatedAt { get; set; }
        public DateTime? LastInteractionAt { get; set; }
        public MatchStatus Status { get; set; }
        public string? Notes { get; set; }

        private UserMatch() { }

        public UserMatch(Guid userId, Guid matchedUserId, float matchScore, MatchType matchType)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            MatchedUserId = matchedUserId;
            MatchScore = Math.Max(0.0f, Math.Min(1.0f, matchScore));
            MatchType = matchType;
            CreatedAt = DateTime.UtcNow;
            Status = MatchStatus.Pending;
        }

        /// <summary>
        /// 更新匹配分数
        /// </summary>
        public void UpdateMatchScore(float newScore)
        {
            MatchScore = Math.Max(0.0f, Math.Min(1.0f, newScore));
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        public void UpdateStatus(MatchStatus newStatus)
        {
            Status = newStatus;
            if (newStatus == MatchStatus.Accepted || newStatus == MatchStatus.Rejected)
            {
                LastInteractionAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 添加备注
        /// </summary>
        public void AddNote(string note)
        {
            Notes = note;
        }
    }
}
