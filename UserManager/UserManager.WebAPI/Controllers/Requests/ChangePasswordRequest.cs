using System;

namespace UserManager.WebAPI.Controllers.Requests
{
    /// <summary>
    /// 修改密码请求模型
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// 新密码
        /// </summary>
        public string Password { get; set; }
    }
}