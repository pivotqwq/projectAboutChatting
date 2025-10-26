using UserManager.Domain.ValueObjects;

namespace UserManager.WebAPI.Controllers.Requests
{
    /// <summary>
    /// 验证码登录请求模型
    /// </summary>
    public class LoginByCodeRequest
    {
        /// <summary>
        /// 用户基本信息（QQ号或手机号）
        /// </summary>
        public UserBasic UserBasic { get; set; }
        
        /// <summary>
        /// 验证码
        /// </summary>
        public string Code { get; set; }
    }
}

