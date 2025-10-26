using MediatR;
using ForumManager.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ForumManager.WebAPI.Events
{
    /// <summary>
    /// 帖子创建事件处理器
    /// </summary>
    public class PostCreatedEventHandler : INotificationHandler<PostCreatedEvent>
    {
        private readonly ILogger<PostCreatedEventHandler> _logger;

        public PostCreatedEventHandler(ILogger<PostCreatedEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("帖子创建事件处理: PostId={PostId}, AuthorId={AuthorId}, Title={Title}", 
                notification.PostId, notification.AuthorId, notification.Title);

            // 这里可以添加具体的业务逻辑，例如：
            // 1. 发送通知给关注者
            // 2. 更新用户统计信息
            // 3. 触发搜索索引更新
            // 4. 发送到消息队列供其他服务消费

            await Task.CompletedTask;
        }
    }
}
