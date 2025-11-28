using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using StackExchange.Redis;

namespace ChatService.Services
{
    /// <summary>
    /// AI聊天机器人服务 - 集成阿里云百炼千问模型
    /// </summary>
    public class AiChatBotService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiChatBotService> _logger;
        private readonly IConnectionMultiplexer _redis;
        private const int DailyTokenLimit = 3000; // 每个用户每天3000 token限制

        public AiChatBotService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AiChatBotService> logger,
            IConnectionMultiplexer redis)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _redis = redis;
        }

        /// <summary>
        /// 获取聊天机器人回复
        /// </summary>
        /// <param name="userId">用户ID（用于token限制）</param>
        /// <param name="userMessage">用户消息</param>
        /// <param name="conversationHistory">对话历史（可选）</param>
        /// <returns>机器人回复</returns>
        public async Task<string?> GetBotResponseAsync(string userId, string userMessage, List<ConversationMessage>? conversationHistory = null)
        {
            var apiKey = _configuration["AiChatBot:Aliyun:ApiKey"];
            var model = _configuration["AiChatBot:Aliyun:Model"] ?? "qwen-turbo";
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("阿里云百炼千问 API Key 未配置，无法使用聊天机器人功能");
                return "抱歉，聊天机器人服务暂不可用，请稍后再试。";
            }

            // 检查用户今日token使用量
            var dailyTokenUsage = await GetDailyTokenUsageAsync(userId);
            if (dailyTokenUsage >= DailyTokenLimit)
            {
                _logger.LogWarning("用户 {UserId} 今日token使用量已达上限: {Usage}/{Limit}", userId, dailyTokenUsage, DailyTokenLimit);
                return "抱歉，您今日的AI对话次数已达上限（3000 token/天），请明天再试。";
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var url = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";

                // 构建消息列表
                var messages = new List<DashScopeMessage>();
                
                // 添加系统提示（可选）
                var systemPrompt = _configuration["AiChatBot:SystemPrompt"] ?? "你是一个友好的AI助手，请用简洁、友好的方式回答用户的问题。";
                messages.Add(new DashScopeMessage { Role = "system", Content = systemPrompt });

                // 添加对话历史
                if (conversationHistory != null && conversationHistory.Count > 0)
                {
                    var botUserId = GetBotUserId();
                    foreach (var historyMsg in conversationHistory)
                    {
                        messages.Add(new DashScopeMessage
                        {
                            Role = historyMsg.FromUserId == botUserId ? "assistant" : "user",
                            Content = historyMsg.Content
                        });
                    }
                }

                // 添加当前用户消息
                messages.Add(new DashScopeMessage { Role = "user", Content = userMessage });

                // 构建请求体（使用compatible-mode格式，与官方示例一致）
                var requestBody = new
                {
                    model = model,
                    messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    try
                    {
                        var result = JsonSerializer.Deserialize<DashScopeCompatibleResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        if (result?.Choices != null && result.Choices.Count > 0)
                        {
                            var reply = result.Choices[0].Message?.Content;
                            var usage = result.Usage;
                            
                            if (!string.IsNullOrWhiteSpace(reply))
                            {
                                // 更新用户token使用量
                                if (usage != null && usage.TotalTokens.HasValue)
                                {
                                    var tokensUsed = usage.TotalTokens.Value;
                                    await IncrementDailyTokenUsageAsync(userId, tokensUsed);
                                }
                                
                                _logger.LogInformation("AI聊天机器人回复成功: UserId={UserId}, TokensUsed={Tokens}", userId, usage?.TotalTokens ?? 0);
                                return reply;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("AI响应格式异常: {Response}", responseContent);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "解析AI响应JSON失败: {Response}", responseContent);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("阿里云百炼千问API调用失败: StatusCode={StatusCode}, Error={Error}", 
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用阿里云百炼千问API异常: UserId={UserId}", userId);
            }

            return "抱歉，我暂时无法回复，请稍后再试。";
        }

        /// <summary>
        /// 获取用户今日token使用量
        /// </summary>
        private async Task<int> GetDailyTokenUsageAsync(string userId)
        {
            try
            {
                var db = _redis.GetDatabase();
                var todayKey = GetDailyTokenKey(userId);
                var usage = await db.StringGetAsync(todayKey);
                return usage.HasValue && int.TryParse(usage, out var value) ? value : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户token使用量失败: UserId={UserId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// 增加用户今日token使用量
        /// </summary>
        private async Task IncrementDailyTokenUsageAsync(string userId, int tokensUsed)
        {
            try
            {
                var db = _redis.GetDatabase();
                var todayKey = GetDailyTokenKey(userId);
                
                // 使用Redis的INCRBY命令增加token使用量
                await db.StringIncrementAsync(todayKey, tokensUsed);
                
                // 设置过期时间为当天结束（UTC时间）
                var tomorrow = DateTime.UtcNow.Date.AddDays(1);
                var expiry = tomorrow - DateTime.UtcNow;
                await db.KeyExpireAsync(todayKey, expiry);
                
                _logger.LogInformation("更新用户token使用量: UserId={UserId}, TokensUsed={Tokens}", userId, tokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户token使用量失败: UserId={UserId}", userId);
            }
        }

        /// <summary>
        /// 获取用户每日token使用的Redis key
        /// </summary>
        private string GetDailyTokenKey(string userId)
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            return $"chatbot:daily-tokens:{userId}:{today}";
        }

        /// <summary>
        /// 检查用户ID是否为聊天机器人
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>是否为机器人</returns>
        public bool IsChatBot(string userId)
        {
            var botUserId = _configuration["AiChatBot:BotUserId"] ?? "ai-chatbot";
            return string.Equals(userId, botUserId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取聊天机器人的用户ID
        /// </summary>
        /// <returns>机器人用户ID</returns>
        public string GetBotUserId()
        {
            return _configuration["AiChatBot:BotUserId"] ?? "ai-chatbot";
        }

        private class DashScopeMessage
        {
            public string Role { get; set; } = string.Empty; // system, user, assistant
            public string Content { get; set; } = string.Empty;
        }

        /// <summary>
        /// Compatible-mode API 响应格式（兼容OpenAI格式）
        /// </summary>
        private class DashScopeCompatibleResponse
        {
            public List<DashScopeChoice>? Choices { get; set; }
            public DashScopeUsage? Usage { get; set; }
            public string? Id { get; set; }
            public string? Model { get; set; }
        }

        private class DashScopeChoice
        {
            public DashScopeMessage? Message { get; set; }
            public string? FinishReason { get; set; }
            public int? Index { get; set; }
        }

        private class DashScopeUsage
        {
            [System.Text.Json.Serialization.JsonPropertyName("prompt_tokens")]
            public int? PromptTokens { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("completion_tokens")]
            public int? CompletionTokens { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("total_tokens")]
            public int? TotalTokens { get; set; }
        }
    }

    /// <summary>
    /// 聊天消息历史记录（用于AI上下文）
    /// </summary>
    public class ConversationMessage
    {
        public string FromUserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

