using MediatR;
using ForumManager.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ForumManager.WebAPI.Events
{
    /// <summary>
    /// 评论添加事件处理器
    /// </summary>
    public class CommentAddedEventHandler : INotificationHandler<CommentAddedEvent>
    {
        private readonly ILogger<CommentAddedEventHandler> _logger;

        public CommentAddedEventHandler(ILogger<CommentAddedEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task Handle(CommentAddedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("评论添加事件处理: CommentId={CommentId}, PostId={PostId}, AuthorId={AuthorId}", 
                notification.CommentId, notification.PostId, notification.AuthorId);

            // 这里可以添加具体的业务逻辑，例如：
            // 1. 发送通知给帖子作者
            // 2. 发送通知给被回复的评论作者
            // 3. 更新帖子热度分数
            // 4. 发送到消息队列供其他服务消费

            await Task.CompletedTask;
        }
    }
}
