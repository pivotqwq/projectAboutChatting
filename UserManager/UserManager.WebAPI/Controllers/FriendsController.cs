using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManager.Infrastracture;
using UserManager.Domain.Entities;

namespace UserManager.WebAPI.Controllers
{
    [ApiController]
    [Route("api/friends")]
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly UserDBContext _db;
        private readonly ILogger<FriendsController> _logger;

        public FriendsController(UserDBContext db, ILogger<FriendsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        private Guid GetCurrentUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var id))
            {
                throw new UnauthorizedAccessException("无效的用户ID");
            }
            return id;
        }

        /// <summary>
        /// 获取我的好友列表（仅返回用户ID列表，展示信息由调用方补充）
        /// </summary>
        [HttpGet]
        public IActionResult GetMyFriends()
        {
            var me = GetCurrentUserId();
            var friendIds = _db.Friendships
                .Where(f => f.UserId == me)
                .Select(f => f.FriendUserId)
                .ToList();
            return Ok(friendIds);
        }

        /// <summary>
        /// 发送好友请求
        /// </summary>
        [HttpPost("request/{receiverId}")]
        public async Task<IActionResult> SendFriendRequest(Guid receiverId)
        {
            var me = GetCurrentUserId();
            if (me == receiverId)
            {
                return BadRequest(new { error = "不能添加自己为好友" });
            }

            // 检查是否已经是好友
            var isFriend = _db.Friendships.Any(f => f.UserId == me && f.FriendUserId == receiverId);
            if (isFriend)
            {
                return BadRequest(new { error = "已经是好友了" });
            }

            // 检查是否已有待处理的请求
            var existingRequest = _db.FriendRequests
                .FirstOrDefault(r => r.RequesterId == me && r.ReceiverId == receiverId && r.Status == "pending");
            if (existingRequest != null)
            {
                return BadRequest(new { error = "已发送好友请求，等待对方处理" });
            }

            // 创建好友请求
            var request = new FriendRequest(me, receiverId);
            _db.FriendRequests.Add(request);
            await _db.SaveChangesAsync();

            return Ok(new { requestId = request.Id, message = "好友请求已发送" });
        }

        /// <summary>
        /// 获取我收到的好友请求列表
        /// </summary>
        [HttpGet("requests/received")]
        public IActionResult GetReceivedFriendRequests()
        {
            var me = GetCurrentUserId();
            var requests = _db.FriendRequests
                .Where(r => r.ReceiverId == me && r.Status == "pending")
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.Id, r.RequesterId, r.Status, r.CreatedAt })
                .ToList();
            return Ok(requests);
        }

        /// <summary>
        /// 获取我发送的好友请求列表
        /// </summary>
        [HttpGet("requests/sent")]
        public IActionResult GetSentFriendRequests()
        {
            var me = GetCurrentUserId();
            var requests = _db.FriendRequests
                .Where(r => r.RequesterId == me)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.Id, r.ReceiverId, r.Status, r.CreatedAt, r.RespondedAt })
                .ToList();
            return Ok(requests);
        }

        /// <summary>
        /// 同意好友请求
        /// </summary>
        [HttpPost("requests/{requestId}/accept")]
        public async Task<IActionResult> AcceptFriendRequest(Guid requestId)
        {
            var me = GetCurrentUserId();
            var request = _db.FriendRequests.FirstOrDefault(r => r.Id == requestId && r.ReceiverId == me);
            if (request == null)
            {
                return NotFound(new { error = "好友请求不存在" });
            }

            if (request.Status != "pending")
            {
                return BadRequest(new { error = "该好友请求已处理" });
            }

            // 同意请求
            request.Accept();

            // 建立双向好友关系
            var exists = _db.Friendships.Any(f => f.UserId == me && f.FriendUserId == request.RequesterId);
            if (!exists)
            {
                _db.Friendships.Add(new Friendship(me, request.RequesterId));
            }
            var existsReverse = _db.Friendships.Any(f => f.UserId == request.RequesterId && f.FriendUserId == me);
            if (!existsReverse)
            {
                _db.Friendships.Add(new Friendship(request.RequesterId, me));
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "已同意好友请求" });
        }

        /// <summary>
        /// 拒绝好友请求
        /// </summary>
        [HttpPost("requests/{requestId}/reject")]
        public async Task<IActionResult> RejectFriendRequest(Guid requestId)
        {
            var me = GetCurrentUserId();
            var request = _db.FriendRequests.FirstOrDefault(r => r.Id == requestId && r.ReceiverId == me);
            if (request == null)
            {
                return NotFound(new { error = "好友请求不存在" });
            }

            if (request.Status != "pending")
            {
                return BadRequest(new { error = "该好友请求已处理" });
            }

            request.Reject();
            await _db.SaveChangesAsync();
            return Ok(new { message = "已拒绝好友请求" });
        }

        /// <summary>
        /// 删除好友（双向移除）
        /// </summary>
        [HttpDelete("{friendUserId}")]
        public async Task<IActionResult> RemoveFriend(Guid friendUserId)
        {
            var me = GetCurrentUserId();
            var list = _db.Friendships.Where(f =>
                (f.UserId == me && f.FriendUserId == friendUserId) ||
                (f.UserId == friendUserId && f.FriendUserId == me));
            _db.Friendships.RemoveRange(list);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}


