using ForumManager.Domain.ValueObjects;
using Zack.Commons;

namespace ForumManager.Domain.Entities
{
    /// <summary>
    /// 帖子实体
    /// </summary>
    public class Post : IAggregateRoot
    {
        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public ValueObjects.PostCategory Category { get; set; }
        public ValueObjects.PostStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int FavoriteCount { get; set; }
        public List<string> Tags { get; set; } = new();

        private readonly List<Comment> _comments = new();
        public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

        private readonly List<PostLike> _likes = new();
        public IReadOnlyCollection<PostLike> Likes => _likes.AsReadOnly();

        private readonly List<PostFavorite> _favorites = new();
        public IReadOnlyCollection<PostFavorite> Favorites => _favorites.AsReadOnly();

        private Post() { }

        public Post(Guid authorId, string title, string content, ValueObjects.PostCategory category, List<string>? tags = null)
        {
            Id = Guid.NewGuid();
            AuthorId = authorId;
            Title = title;
            Content = content;
            Category = category;
            Status = ValueObjects.PostStatus.Published;
            CreatedAt = DateTime.UtcNow;
            ViewCount = 0;
            LikeCount = 0;
            CommentCount = 0;
            FavoriteCount = 0;
            Tags = tags ?? new List<string>();
        }

        /// <summary>
        /// 编辑帖子
        /// </summary>
        public void EditPost(string title, string content, List<string>? tags = null)
        {
            if (Status == ValueObjects.PostStatus.Deleted)
                throw new InvalidOperationException("无法编辑已删除的帖子");

            Title = title;
            Content = content;
            Tags = tags ?? new List<string>();
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 删除帖子
        /// </summary>
        public void DeletePost()
        {
            Status = ValueObjects.PostStatus.Deleted;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 增加浏览次数
        /// </summary>
        public void IncrementViewCount()
        {
            ViewCount++;
        }

        /// <summary>
        /// 添加评论
        /// </summary>
        public void AddComment(Comment comment)
        {
            _comments.Add(comment);
            CommentCount++;
        }

        /// <summary>
        /// 删除评论
        /// </summary>
        public void RemoveComment(Guid commentId)
        {
            var comment = _comments.FirstOrDefault(c => c.Id == commentId);
            if (comment != null)
            {
                _comments.Remove(comment);
                CommentCount--;
            }
        }

        /// <summary>
        /// 点赞帖子
        /// </summary>
        public void LikePost(Guid userId)
        {
            if (_likes.Any(l => l.UserId == userId))
                return; // 已经点赞过了

            _likes.Add(new PostLike(Id, userId));
            LikeCount++;
        }

        /// <summary>
        /// 取消点赞
        /// </summary>
        public void UnlikePost(Guid userId)
        {
            var like = _likes.FirstOrDefault(l => l.UserId == userId);
            if (like != null)
            {
                _likes.Remove(like);
                LikeCount--;
            }
        }

        /// <summary>
        /// 收藏帖子
        /// </summary>
        public void FavoritePost(Guid userId)
        {
            if (_favorites.Any(f => f.UserId == userId))
                return; // 已经收藏过了

            _favorites.Add(new PostFavorite(Id, userId));
            FavoriteCount++;
        }

        /// <summary>
        /// 取消收藏
        /// </summary>
        public void UnfavoritePost(Guid userId)
        {
            var favorite = _favorites.FirstOrDefault(f => f.UserId == userId);
            if (favorite != null)
            {
                _favorites.Remove(favorite);
                FavoriteCount--;
            }
        }
    }
}
