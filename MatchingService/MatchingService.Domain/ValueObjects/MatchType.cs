namespace MatchingService.Domain.ValueObjects
{
    /// <summary>
    /// 匹配类型枚举
    /// </summary>
    public enum MatchType
    {
        /// <summary>
        /// 基于标签的匹配
        /// </summary>
        TagBased = 1,

        /// <summary>
        /// 基于协同过滤的匹配
        /// </summary>
        CollaborativeFiltering = 2,

        /// <summary>
        /// 基于地理位置的匹配
        /// </summary>
        LocationBased = 3,

        /// <summary>
        /// 基于活跃度的匹配
        /// </summary>
        ActivityBased = 4,

        /// <summary>
        /// 混合推荐
        /// </summary>
        Hybrid = 5,

        /// <summary>
        /// 随机推荐
        /// </summary>
        Random = 6
    }
}
