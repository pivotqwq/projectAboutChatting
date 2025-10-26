using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManager.Domain.ValueObjects
{
    /// <summary>
    /// 用户基本信息值对象
    /// </summary>
    /// <param name="PhoneNumber">手机号</param>
    /// <param name="Email">邮箱</param>
    public record UserBasic(string? PhoneNumber, string? Email);
}
