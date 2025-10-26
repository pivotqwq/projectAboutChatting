using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManager.Domain.ValueObjects;
using UserManager.Domain;

namespace UserManager.Infrastracture
{
    public class SmsCodeSender : ISmsCodeSender
    {
        public Task SendCodeAsync(UserBasic userBasic, string code)
        {
            // 实际应用中，这里应该调用真实的短信服务API
            // 现在我们只是记录日志，表示短信已发送
            Console.WriteLine($"向用户 {userBasic.PhoneNumber} 发送验证码: {code}");
            return Task.CompletedTask;
        }
    }
}
