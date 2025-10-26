using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using System.Security.Claims;
using MongoDB.Bson;
using ChatService.Services;

namespace ChatService.Hubs
{
    /// <summary>
    /// 聊天中心 - 处理实时消息推送和在线状态管理
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMongoDatabase _database;
        private readonly MessageRepository _messageRepository;
        private readonly OnlineEventPublisher _onlinePublisher;
        private readonly PresenceService _presenceService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IMongoDatabase database, 
            MessageRepository messageRepository, 
            OnlineEventPublisher onlinePublisher, 
            PresenceService presenceService,
            ILogger<ChatHub> logger)
        {
            _database = database;
            _messageRepository = messageRepository;
            _onlinePublisher = onlinePublisher;
            _presenceService = presenceService;
            _logger = logger;
        }

        /// <summary>
        /// 用户连接时触发 - 自动注册在线状态
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _presenceService.AddConnectionAsync(userId, Context.ConnectionId);
                await _onlinePublisher.PublishUserOnlineAsync(userId);
                _logger.LogInformation("用户 {UserId} 已连接，ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("连接用户身份验证失败，ConnectionId: {ConnectionId}", Context.ConnectionId);
            }
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 用户断开连接时触发 - 自动更新在线状态
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var becameOffline = await _presenceService.RemoveConnectionAsync(userId, Context.ConnectionId);
                if (becameOffline)
                {
                    await _onlinePublisher.PublishUserOfflineAsync(userId);
                    _logger.LogInformation("用户 {UserId} 已完全离线", userId);
                }
                else
                {
                    _logger.LogInformation("用户 {UserId} 断开一个连接，但仍有其他活跃连接", userId);
                }
            }

            if (exception != null)
            {
                _logger.LogError(exception, "用户 {UserId} 断开连接时发生异常", userId ?? "unknown");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        /// <param name="toUserId">接收者用户ID</param>
        /// <param name="content">消息内容</param>
        public async Task SendPrivateMessage(string toUserId, string content)
        {
            try
            {
                var fromUserId = GetUserId();
                if (string.IsNullOrWhiteSpace(fromUserId))
                {
                    throw new HubException("未授权：无法获取用户身份");
                }

                if (string.IsNullOrWhiteSpace(toUserId))
                {
                    throw new HubException("接收者用户ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new HubException("消息内容不能为空");
                }

                if (content.Length > 5000)
                {
                    throw new HubException("消息内容超过最大长度限制（5000字符）");
                }

                var msg = ChatMessage.CreatePrivate(fromUserId, toUserId, content);
                await _messageRepository.InsertAsync(msg.ToBsonDocument());

                // 发送给接收者
                await Clients.User(toUserId).SendAsync("ReceivePrivateMessage", msg);
                // 发送确认给发送者
                await Clients.User(fromUserId).SendAsync("PrivateMessageSent", msg);

                _logger.LogInformation("私聊消息已发送: {FromUserId} -> {ToUserId}, MsgId: {MessageId}", 
                    fromUserId, toUserId, msg.Id);
            }
            catch (HubException)
            {
                throw; // 重新抛出 HubException 以便客户端能接收到错误消息
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送私聊消息失败");
                throw new HubException("发送消息失败，请稍后重试");
            }
        }

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="content">消息内容</param>
        public async Task SendGroupMessage(string groupId, string content)
        {
            try
            {
                var fromUserId = GetUserId();
                if (string.IsNullOrWhiteSpace(fromUserId))
                {
                    throw new HubException("未授权：无法获取用户身份");
                }

                if (string.IsNullOrWhiteSpace(groupId))
                {
                    throw new HubException("群组ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new HubException("消息内容不能为空");
                }

                if (content.Length > 5000)
                {
                    throw new HubException("消息内容超过最大长度限制（5000字符）");
                }

                var msg = ChatMessage.CreateGroup(fromUserId, groupId, content);
                await _messageRepository.InsertAsync(msg.ToBsonDocument());

                // 发送给群组所有成员
                await Clients.Group(groupId).SendAsync("ReceiveGroupMessage", msg);

                _logger.LogInformation("群聊消息已发送: {FromUserId} -> Group:{GroupId}, MsgId: {MessageId}", 
                    fromUserId, groupId, msg.Id);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送群聊消息失败");
                throw new HubException("发送消息失败，请稍后重试");
            }
        }

        /// <summary>
        /// 加入群组
        /// </summary>
        /// <param name="groupId">群组ID</param>
        public async Task JoinGroup(string groupId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    throw new HubException("群组ID不能为空");
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
                
                var userId = GetUserId();
                _logger.LogInformation("用户 {UserId} 加入群组 {GroupId}", userId ?? "unknown", groupId);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加入群组失败，GroupId: {GroupId}", groupId);
                throw new HubException("加入群组失败，请稍后重试");
            }
        }

        /// <summary>
        /// 离开群组
        /// </summary>
        /// <param name="groupId">群组ID</param>
        public async Task LeaveGroup(string groupId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    throw new HubException("群组ID不能为空");
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
                
                var userId = GetUserId();
                _logger.LogInformation("用户 {UserId} 离开群组 {GroupId}", userId ?? "unknown", groupId);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "离开群组失败，GroupId: {GroupId}", groupId);
                throw new HubException("离开群组失败，请稍后重试");
            }
        }

        /// <summary>
        /// 从 JWT Claims 中获取用户ID
        /// </summary>
        private string GetUserId()
        {
            // UserManager使用 "sub" claim 存储用户ID
            return Context.User?.FindFirstValue("sub")  // JWT标准的Subject claim
                   ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) 
                   ?? Context.UserIdentifier 
                   ?? Context.User?.Identity?.Name 
                   ?? string.Empty;
        }
    }

    /// <summary>
    /// 聊天消息模型
    /// </summary>
    public record ChatMessage
    (
        string Id,
        string Type,
        string FromUserId,
        string? ToUserId,
        string? GroupId,
        string Content,
        DateTime CreatedAt
    )
    {
        /// <summary>
        /// 创建私聊消息
        /// </summary>
        public static ChatMessage CreatePrivate(string fromUserId, string toUserId, string content)
            => new(Guid.NewGuid().ToString("N"), "private", fromUserId, toUserId, null, content, DateTime.UtcNow);

        /// <summary>
        /// 创建群聊消息
        /// </summary>
        public static ChatMessage CreateGroup(string fromUserId, string groupId, string content)
            => new(Guid.NewGuid().ToString("N"), "group", fromUserId, null, groupId, content, DateTime.UtcNow);
    }
}


