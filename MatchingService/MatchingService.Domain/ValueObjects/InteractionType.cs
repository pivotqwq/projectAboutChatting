namespace MatchingService.Domain.ValueObjects
{
    /// <summary>
    /// 交互类型枚举
    /// </summary>
    public enum InteractionType
    {
        /// <summary>
        /// 查看资料
        /// </summary>
        ViewProfile = 1,

        /// <summary>
        /// 发送消息
        /// </summary>
        SendMessage = 2,

        /// <summary>
        /// 点赞
        /// </summary>
        Like = 3,

        /// <summary>
        /// 关注
        /// </summary>
        Follow = 4,

        /// <summary>
        /// 评论
        /// </summary>
        Comment = 5,

        /// <summary>
        /// 分享
        /// </summary>
        Share = 6,

        /// <summary>
        /// 举报
        /// </summary>
        Report = 7,

        /// <summary>
        /// 加入群组
        /// </summary>
        JoinGroup = 8,

        /// <summary>
        /// 参与活动
        /// </summary>
        JoinActivity = 9
    }
}
