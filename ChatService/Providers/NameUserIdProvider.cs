using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ChatService.Providers
{
    /// <summary>
    /// 从 JWT "sub" claim 中获取用户ID，用于SignalR的用户路由
    /// </summary>
    public class NameUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // ⚠️ 必须与 ChatHub.GetUserId() 的逻辑一致！
            // UserManager使用 "sub" claim 存储用户ID
            return connection.User?.FindFirstValue("sub")  // JWT标准的Subject claim
                   ?? connection.User?.FindFirstValue(ClaimTypes.NameIdentifier) 
                   ?? connection.User?.Identity?.Name;
        }
    }
}


