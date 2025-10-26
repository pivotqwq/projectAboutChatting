using UserManager.Domain.ValueObjects;

namespace UserManager.WebAPI.Controllers.Requests
{
    /// <summary>
    /// 发送验证码请求模型
    /// </summary>
    public class SendCodeRequest
    {
        /// <summary>
        /// 用户基本信息（QQ号或手机号）
        /// </summary>
        public UserBasic UserBasic { get; set; }
    }
}

