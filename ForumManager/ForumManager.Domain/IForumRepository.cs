using ForumManager.Domain.Entities;

namespace ForumManager.Domain
{
    /// <summary>
    /// 论坛仓储接口
    /// </summary>
    public interface IForumRepository
    {
        // 帖子相关
        Task<Post?> GetPostByIdAsync(Guid postId);
        Task<List<Post>> GetPostsAsync(int pageIndex, int pageSize, ValueObjects.PostCategory? category = null, string? keyword = null);
        Task<List<Post>> SearchPostsByTitleAsync(string titleKeyword, int pageIndex, int pageSize, ValueObjects.PostCategory? category = null);
        Task<List<Post>> GetHotPostsAsync(int count);
        Task<List<Post>> GetUserPostsAsync(Guid userId, int pageIndex, int pageSize);
        Task<Post> CreatePostAsync(Post post);
        Task<Post> UpdatePostAsync(Post post);
        Task DeletePostAsync(Guid postId);

        // 评论相关
        Task<Comment?> GetCommentByIdAsync(Guid commentId);
        Task<List<Comment>> GetPostCommentsAsync(Guid postId, int pageIndex, int pageSize);
        Task<Comment> CreateCommentAsync(Comment comment);
        Task<Comment> UpdateCommentAsync(Comment comment);
        Task DeleteCommentAsync(Guid commentId);

        // 点赞相关
        Task<bool> IsPostLikedByUserAsync(Guid postId, Guid userId);
        Task<bool> IsCommentLikedByUserAsync(Guid commentId, Guid userId);
        Task<bool> IsPostFavoritedByUserAsync(Guid postId, Guid userId);
        Task TogglePostLikeAsync(Guid postId, Guid userId);
        Task TogglePostFavoriteAsync(Guid postId, Guid userId);
        Task ToggleCommentLikeAsync(Guid commentId, Guid userId);

        // 收藏相关
        Task<List<Guid>> GetFavoritePostIdsByUserAsync(Guid userId);

        // 统计相关
        Task<int> GetPostCountAsync(ValueObjects.PostCategory? category = null);
        Task<int> GetPostCountByTitleAsync(string titleKeyword, ValueObjects.PostCategory? category = null);
        Task<int> GetCommentCountAsync(Guid postId);
        Task<int> GetLikeCountAsync(Guid postId);
        Task<int> GetFavoriteCountAsync(Guid postId);
    }
}
