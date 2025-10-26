using System.Text.Json;

namespace ApiGateway.Services;

public interface IAggregationService
{
    Task<JsonDocument> GetUserProfileWithPostsAsync(string userId, int page = 1, int pageSize = 10);
    Task<JsonDocument> GetPostWithCommentsAsync(string postId, int page = 1, int pageSize = 10);
    Task<JsonDocument> GetUserDashboardAsync(string userId);
    Task<JsonDocument> GetForumStatsAsync();
}
