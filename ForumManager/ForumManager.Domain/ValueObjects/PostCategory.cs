namespace ForumManager.Domain.ValueObjects
{
    /// <summary>
    /// 帖子分类枚举
    /// </summary>
    public enum PostCategory
    {
        /// <summary>
        /// 找学习搭子
        /// </summary>
        StudyPartner = 1,

        /// <summary>
        /// 运动组队
        /// </summary>
        SportsTeam = 2,

        /// <summary>
        /// 技术讨论
        /// </summary>
        TechDiscussion = 3,

        /// <summary>
        /// 生活分享
        /// </summary>
        LifeSharing = 4,

        /// <summary>
        /// 求职招聘
        /// </summary>
        JobHunting = 5,

        /// <summary>
        /// 其他
        /// </summary>
        Other = 99
    }

    /// <summary>
    /// 帖子状态枚举
    /// </summary>
    public enum PostStatus
    {
        /// <summary>
        /// 草稿
        /// </summary>
        Draft = 1,

        /// <summary>
        /// 已发布
        /// </summary>
        Published = 2,

        /// <summary>
        /// 已删除
        /// </summary>
        Deleted = 3,

        /// <summary>
        /// 已隐藏
        /// </summary>
        Hidden = 4
    }
}
