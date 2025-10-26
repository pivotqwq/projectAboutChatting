namespace UserManager.WebAPI.Controllers.Requests
{
    /// <summary>
    /// 锁定用户请求
    /// </summary>
    public class LockUserRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// 锁定时长（分钟），0表示永久锁定
        /// </summary>
        public int LockMinutes { get; set; }
        
        /// <summary>
        /// 锁定原因
        /// </summary>
        public string? Reason { get; set; }
    }
}
