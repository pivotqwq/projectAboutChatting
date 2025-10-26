namespace MatchingService.Domain.ValueObjects
{
    /// <summary>
    /// 匹配状态枚举
    /// </summary>
    public enum MatchStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending = 1,

        /// <summary>
        /// 已接受
        /// </summary>
        Accepted = 2,

        /// <summary>
        /// 已拒绝
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// 已过期
        /// </summary>
        Expired = 4,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 5
    }
}
