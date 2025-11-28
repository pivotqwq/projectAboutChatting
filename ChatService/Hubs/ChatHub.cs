using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using System.Security.Claims;
using MongoDB.Bson;
using ChatService.Services;
using System.Net.Http.Json;
using System.Net.Http.Headers;

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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly GroupMemberCache _groupMemberCache;
        private readonly AiChatBotService _chatBotService;

        public ChatHub(
            IMongoDatabase database, 
            MessageRepository messageRepository, 
            OnlineEventPublisher onlinePublisher, 
            PresenceService presenceService,
            ILogger<ChatHub> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            GroupMemberCache groupMemberCache,
            AiChatBotService chatBotService)
        {
            _database = database;
            _messageRepository = messageRepository;
            _onlinePublisher = onlinePublisher;
            _presenceService = presenceService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _groupMemberCache = groupMemberCache;
            _chatBotService = chatBotService;
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

                // 如果发送给聊天机器人，触发AI回复
                if (_chatBotService.IsChatBot(toUserId))
                {
                    _ = Task.Run(async () => await ProcessChatBotMessageAsync(fromUserId, toUserId, content, msg.Id));
                }
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

                // 成员校验
                var isMember = await IsUserGroupMemberAsync(groupId, fromUserId);
                if (!isMember)
                {
                    throw new HubException("您不是该群成员，无法发送消息");
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

                var userId = GetUserId();
                // 成员校验（允许非成员加入需看业务，这里要求必须已加入群）
                var isMember = await IsUserGroupMemberAsync(groupId, userId);
                if (!isMember)
                {
                    throw new HubException("您不是该群成员，无法加入");
                }
                await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
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
        /// 加入频道（公屏聊天）
        /// </summary>
        /// <param name="channelId">频道ID</param>
        public async Task JoinChannel(string channelId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(channelId))
                {
                    throw new HubException("频道ID不能为空");
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
                
                var userId = GetUserId();
                _logger.LogInformation("用户 {UserId} 加入频道 {ChannelId}", userId ?? "unknown", channelId);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加入频道失败，ChannelId: {ChannelId}", channelId);
                throw new HubException("加入频道失败，请稍后重试");
            }
        }

        /// <summary>
        /// 离开频道
        /// </summary>
        /// <param name="channelId">频道ID</param>
        public async Task LeaveChannel(string channelId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(channelId))
                {
                    throw new HubException("频道ID不能为空");
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
                
                var userId = GetUserId();
                _logger.LogInformation("用户 {UserId} 离开频道 {ChannelId}", userId ?? "unknown", channelId);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "离开频道失败，ChannelId: {ChannelId}", channelId);
                throw new HubException("离开频道失败，请稍后重试");
            }
        }

        /// <summary>
        /// 发送私聊语音消息
        /// </summary>
        /// <param name="toUserId">接收者用户ID</param>
        /// <param name="voiceFilePath">语音文件路径（相对路径）</param>
        /// <param name="voiceFileUrl">语音文件访问URL</param>
        /// <param name="duration">语音时长（秒）</param>
        /// <param name="recognizedText">语音识别的文字（可选）</param>
        public async Task SendPrivateVoiceMessage(string toUserId, string voiceFilePath, string? voiceFileUrl = null, int? duration = null, string? recognizedText = null)
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

                if (string.IsNullOrWhiteSpace(voiceFilePath))
                {
                    throw new HubException("语音文件路径不能为空");
                }

                var msg = ChatMessage.CreatePrivateVoice(fromUserId, toUserId, voiceFilePath, voiceFileUrl, duration, recognizedText);
                await _messageRepository.InsertAsync(msg.ToBsonDocument());

                // 发送给接收者
                await Clients.User(toUserId).SendAsync("ReceivePrivateMessage", msg);
                // 发送确认给发送者
                await Clients.User(fromUserId).SendAsync("PrivateMessageSent", msg);

                _logger.LogInformation("私聊语音消息已发送: {FromUserId} -> {ToUserId}, MsgId: {MessageId}", 
                    fromUserId, toUserId, msg.Id);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送私聊语音消息失败");
                throw new HubException("发送语音消息失败，请稍后重试");
            }
        }

        /// <summary>
        /// 发送群聊语音消息
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="voiceFilePath">语音文件路径（相对路径）</param>
        /// <param name="voiceFileUrl">语音文件访问URL</param>
        /// <param name="duration">语音时长（秒）</param>
        /// <param name="recognizedText">语音识别的文字（可选）</param>
        public async Task SendGroupVoiceMessage(string groupId, string voiceFilePath, string? voiceFileUrl = null, int? duration = null, string? recognizedText = null)
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

                // 成员校验
                var isMember = await IsUserGroupMemberAsync(groupId, fromUserId);
                if (!isMember)
                {
                    throw new HubException("您不是该群成员，无法发送消息");
                }

                if (string.IsNullOrWhiteSpace(voiceFilePath))
                {
                    throw new HubException("语音文件路径不能为空");
                }

                var msg = ChatMessage.CreateGroupVoice(fromUserId, groupId, voiceFilePath, voiceFileUrl, duration, recognizedText);
                await _messageRepository.InsertAsync(msg.ToBsonDocument());

                // 发送给群组所有成员
                await Clients.Group(groupId).SendAsync("ReceiveGroupMessage", msg);

                _logger.LogInformation("群聊语音消息已发送: {FromUserId} -> Group:{GroupId}, MsgId: {MessageId}", 
                    fromUserId, groupId, msg.Id);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送群聊语音消息失败");
                throw new HubException("发送语音消息失败，请稍后重试");
            }
        }

        /// <summary>
        /// 发送频道语音消息（公屏聊天）
        /// </summary>
        /// <param name="channelId">频道ID</param>
        /// <param name="voiceFilePath">语音文件路径（相对路径）</param>
        /// <param name="voiceFileUrl">语音文件访问URL</param>
        /// <param name="duration">语音时长（秒）</param>
        /// <param name="recognizedText">语音识别的文字（可选）</param>
        public async Task SendChannelVoiceMessage(string channelId, string voiceFilePath, string? voiceFileUrl = null, int? duration = null, string? recognizedText = null)
        {
            try
            {
                var fromUserId = GetUserId();
                if (string.IsNullOrWhiteSpace(fromUserId))
                {
                    throw new HubException("未授权：无法获取用户身份");
                }

                if (string.IsNullOrWhiteSpace(channelId))
                {
                    throw new HubException("频道ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(voiceFilePath))
                {
                    throw new HubException("语音文件路径不能为空");
                }

                var msg = ChatMessage.CreateChannelVoice(fromUserId, channelId, voiceFilePath, voiceFileUrl, duration, recognizedText);
                await _messageRepository.InsertAsync(msg.ToBsonDocument());

                // 发送给频道所有成员
                await Clients.Group(channelId).SendAsync("ReceiveChannelMessage", msg);

                _logger.LogInformation("频道语音消息已发送: {FromUserId} -> Channel:{ChannelId}, MsgId: {MessageId}", 
                    fromUserId, channelId, msg.Id);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送频道语音消息失败");
                throw new HubException("发送语音消息失败，请稍后重试");
            }
        }

        /// <summary>
        /// 发送频道消息（公屏聊天）
        /// </summary>
        /// <param name="channelId">频道ID</param>
        /// <param name="content">消息内容</param>
        public async Task SendChannelMessage(string channelId, string content)
        {
            try
            {
                var fromUserId = GetUserId();
                if (string.IsNullOrWhiteSpace(fromUserId))
                {
                    throw new HubException("未授权：无法获取用户身份");
                }

                if (string.IsNullOrWhiteSpace(channelId))
                {
                    throw new HubException("频道ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new HubException("消息内容不能为空");
                }

                if (content.Length > 5000)
                {
                    throw new HubException("消息内容超过最大长度限制（5000字符）");
                }

                var msg = ChatMessage.CreateChannel(fromUserId, channelId, content);
                await _messageRepository.InsertAsync(msg.ToBsonDocument());

                // 发送给频道所有成员
                await Clients.Group(channelId).SendAsync("ReceiveChannelMessage", msg);

                _logger.LogInformation("频道消息已发送: {FromUserId} -> Channel:{ChannelId}, MsgId: {MessageId}", 
                    fromUserId, channelId, msg.Id);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送频道消息失败");
                throw new HubException("发送消息失败，请稍后重试");
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

        /// <summary>
        /// 处理发送给聊天机器人的消息，获取AI回复并发送
        /// </summary>
        private async Task ProcessChatBotMessageAsync(string fromUserId, string toUserId, string userMessage, string messageId)
        {
            try
            {
                _logger.LogInformation("开始处理聊天机器人消息: FromUserId={FromUserId}, MessageId={MessageId}", 
                    fromUserId, messageId);

                // 获取最近的对话历史（可选，用于上下文）
                var conversationHistory = await GetRecentConversationHistoryAsync(fromUserId, toUserId, maxMessages: 10);

                // 调用AI服务获取回复（传入用户ID用于token限制）
                var botResponse = await _chatBotService.GetBotResponseAsync(fromUserId, userMessage, conversationHistory);

                if (!string.IsNullOrWhiteSpace(botResponse))
                {
                    // 创建机器人回复消息
                    var botMsg = ChatMessage.CreatePrivate(toUserId, fromUserId, botResponse);
                    await _messageRepository.InsertAsync(botMsg.ToBsonDocument());

                    // 发送回复给用户
                    await Clients.User(fromUserId).SendAsync("ReceivePrivateMessage", botMsg);

                    _logger.LogInformation("聊天机器人回复已发送: BotUserId={BotUserId} -> UserId={UserId}, MsgId={MessageId}", 
                        toUserId, fromUserId, botMsg.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理聊天机器人消息失败: FromUserId={FromUserId}, MessageId={MessageId}", 
                    fromUserId, messageId);
            }
        }

        /// <summary>
        /// 获取最近的对话历史（用于AI上下文）
        /// </summary>
        private async Task<List<Services.ConversationMessage>> GetRecentConversationHistoryAsync(string userA, string userB, int maxMessages = 10)
        {
            try
            {
                var messages = await _messageRepository.GetPrivateHistoryAsync(userA, userB, null, maxMessages);
                return messages.Select(doc =>
                {
                    var fromUserId = doc.GetValue("FromUserId", defaultValue: MongoDB.Bson.BsonNull.Value)?.ToString() ?? string.Empty;
                    var content = doc.GetValue("Content", defaultValue: MongoDB.Bson.BsonNull.Value)?.ToString() ?? string.Empty;
                    var createdAt = doc.GetValue("CreatedAt", defaultValue: MongoDB.Bson.BsonNull.Value).ToUniversalTime();

                    return new Services.ConversationMessage
                    {
                        FromUserId = fromUserId,
                        Content = content,
                        CreatedAt = createdAt
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取对话历史失败，将使用空历史");
                return new List<Services.ConversationMessage>();
            }
        }

        private async Task<bool> IsUserGroupMemberAsync(string groupId, string userId)
        {
            if (!Guid.TryParse(groupId, out var gid) || !Guid.TryParse(userId, out var uid))
            {
                return false;
            }
            
            // 先尝试从缓存获取
            var cachedResult = await _groupMemberCache.IsMemberFromCacheAsync(groupId, userId);
            if (cachedResult.HasValue)
            {
                return cachedResult.Value; // 缓存命中，直接返回
            }

            // 缓存未命中，调用 API 并更新缓存
            try
            {
                var baseUrl = _configuration["UserManager:BaseUrl"] ?? "http://localhost:9291";
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5); // 减少超时时间
                
                // 从 SignalR Context 中获取 JWT token
                var httpContext = Context.GetHttpContext();
                string? token = null;
                
                if (httpContext != null)
                {
                    token = httpContext.Request.Query["access_token"].FirstOrDefault();
                    if (string.IsNullOrEmpty(token))
                    {
                        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            token = authHeader.Substring("Bearer ".Length).Trim();
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }
                
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                var url = $"{baseUrl}/api/groups/{gid}/members";
                var response = await client.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("调用 UserManager API 失败: StatusCode={StatusCode}, GroupId={GroupId}", 
                        response.StatusCode, groupId);
                    return false;
                }
                
                var members = await response.Content.ReadFromJsonAsync<List<MemberDto>>();
                
                if (members == null || members.Count == 0)
                {
                    return false;
                }
                
                // 更新缓存
                var memberIds = members.Select(m => m.UserId).ToList();
                await _groupMemberCache.CacheGroupMembersAsync(groupId, memberIds);
                
                // 检查是否为成员
                var isMember = members.Any(m => m.UserId == uid);
                return isMember;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("调用 UserManager API 超时: GroupId={GroupId}", groupId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "校验群成员失败: GroupId={GroupId}, UserId={UserId}", groupId, userId);
                return false;
            }
        }

        private class MemberDto
        {
            public Guid UserId { get; set; }
            public string? Role { get; set; }
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
        DateTime CreatedAt,
        string? MessageType = null,  // "text" 或 "voice"
        string? VoiceFilePath = null,  // 语音文件路径（相对路径）
        string? VoiceFileUrl = null,   // 语音文件访问URL
        int? VoiceDuration = null,     // 语音时长（秒）
        string? RecognizedText = null  // 语音识别的文字
    )
    {
        /// <summary>
        /// 创建私聊文本消息
        /// </summary>
        public static ChatMessage CreatePrivate(string fromUserId, string toUserId, string content)
            => new(Guid.NewGuid().ToString("N"), "private", fromUserId, toUserId, null, content, DateTime.UtcNow, "text");

        /// <summary>
        /// 创建私聊语音消息
        /// </summary>
        public static ChatMessage CreatePrivateVoice(string fromUserId, string toUserId, string voiceFilePath, string? voiceFileUrl = null, int? duration = null, string? recognizedText = null)
            => new(Guid.NewGuid().ToString("N"), "private", fromUserId, toUserId, null, recognizedText ?? string.Empty, DateTime.UtcNow, "voice", voiceFilePath, voiceFileUrl, duration, recognizedText);

        /// <summary>
        /// 创建群聊文本消息
        /// </summary>
        public static ChatMessage CreateGroup(string fromUserId, string groupId, string content)
            => new(Guid.NewGuid().ToString("N"), "group", fromUserId, null, groupId, content, DateTime.UtcNow, "text");

        /// <summary>
        /// 创建群聊语音消息
        /// </summary>
        public static ChatMessage CreateGroupVoice(string fromUserId, string groupId, string voiceFilePath, string? voiceFileUrl = null, int? duration = null, string? recognizedText = null)
            => new(Guid.NewGuid().ToString("N"), "group", fromUserId, null, groupId, recognizedText ?? string.Empty, DateTime.UtcNow, "voice", voiceFilePath, voiceFileUrl, duration, recognizedText);

        /// <summary>
        /// 创建频道文本消息
        /// </summary>
        public static ChatMessage CreateChannel(string fromUserId, string channelId, string content)
            => new(Guid.NewGuid().ToString("N"), "channel", fromUserId, null, channelId, content, DateTime.UtcNow, "text");

        /// <summary>
        /// 创建频道语音消息
        /// </summary>
        public static ChatMessage CreateChannelVoice(string fromUserId, string channelId, string voiceFilePath, string? voiceFileUrl = null, int? duration = null, string? recognizedText = null)
            => new(Guid.NewGuid().ToString("N"), "channel", fromUserId, null, channelId, recognizedText ?? string.Empty, DateTime.UtcNow, "voice", voiceFilePath, voiceFileUrl, duration, recognizedText);

        /// <summary>
        /// 转换为 BsonDocument
        /// </summary>
        public BsonDocument ToBsonDocument()
        {
            var doc = new BsonDocument
            {
                { "_id", Id },
                { "Id", Id },
                { "Type", Type },
                { "FromUserId", FromUserId },
                { "ToUserId", ToUserId != null ? (BsonValue)ToUserId : BsonNull.Value },
                { "GroupId", GroupId != null ? (BsonValue)GroupId : BsonNull.Value },
                { "Content", Content },
                { "CreatedAt", CreatedAt }
            };

            // 添加语音消息相关字段
            if (!string.IsNullOrWhiteSpace(MessageType))
            {
                doc.Add("MessageType", MessageType);
            }
            if (!string.IsNullOrWhiteSpace(VoiceFilePath))
            {
                doc.Add("VoiceFilePath", VoiceFilePath);
            }
            if (!string.IsNullOrWhiteSpace(VoiceFileUrl))
            {
                doc.Add("VoiceFileUrl", VoiceFileUrl);
            }
            if (VoiceDuration.HasValue)
            {
                doc.Add("VoiceDuration", VoiceDuration.Value);
            }
            if (!string.IsNullOrWhiteSpace(RecognizedText))
            {
                doc.Add("RecognizedText", RecognizedText);
            }

            return doc;
        }
    }
}


