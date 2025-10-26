namespace MatchingService.Domain.ValueObjects
{
    /// <summary>
    /// 推荐结果值对象
    /// </summary>
    public record RecommendationResult
    {
        public Guid UserId { get; init; }
        public float Score { get; init; }
        public MatchType MatchType { get; init; }
        public string Reason { get; init; } // 推荐理由
        public Dictionary<string, object> Metadata { get; init; } // 额外元数据

        public RecommendationResult(Guid userId, float score, MatchType matchType, string reason, Dictionary<string, object>? metadata = null)
        {
            UserId = userId;
            Score = Math.Max(0.0f, Math.Min(1.0f, score));
            MatchType = matchType;
            Reason = reason;
            Metadata = metadata ?? new Dictionary<string, object>();
        }
    }
}
