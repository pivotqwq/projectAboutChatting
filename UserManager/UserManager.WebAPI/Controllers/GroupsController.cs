using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UserManager.Infrastracture;
using UserManager.Domain.Entities;

namespace UserManager.WebAPI.Controllers
{
    [ApiController]
    [Route("api/groups")]
    [Authorize]
    public class GroupsController : ControllerBase
    {
        private readonly UserDBContext _db;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(UserDBContext db, ILogger<GroupsController> logger)
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
        /// 获取我加入的群组列表
        /// </summary>
        [HttpGet("mine")]
        public IActionResult GetMyGroups()
        {
            var me = GetCurrentUserId();
            var groups = from gm in _db.GroupMembers
                         join g in _db.Groups on gm.GroupId equals g.Id
                         where gm.UserId == me
                         select new { g.Id, g.Name, g.OwnerId, g.CreatedAt, gm.Role };
            return Ok(groups.ToList());
        }

        /// <summary>
        /// 创建群组（创建者为owner并加入成员）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Name))
            {
                return BadRequest(new { error = "群组名称不能为空" });
            }
            var me = GetCurrentUserId();
            var g = new Group(req.Name, me);
            _db.Groups.Add(g);
            _db.GroupMembers.Add(new GroupMember(g.Id, me, role: "owner"));
            await _db.SaveChangesAsync();
            return Ok(new { g.Id, g.Name, g.OwnerId, g.CreatedAt });
        }

        /// <summary>
        /// 获取群组成员列表
        /// </summary>
        [HttpGet("{groupId}/members")]
        public IActionResult GetMembers(Guid groupId)
        {
            var members = _db.GroupMembers.Where(m => m.GroupId == groupId)
                .Select(m => new { m.UserId, m.Role, m.Nickname, m.JoinedAt })
                .ToList();
            return Ok(members);
        }

        /// <summary>
        /// 发送群聊加入申请
        /// </summary>
        [HttpPost("{groupId}/request")]
        public async Task<IActionResult> SendJoinRequest(Guid groupId)
        {
            var me = GetCurrentUserId();

            // 检查群组是否存在
            var group = _db.Groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null)
            {
                return NotFound(new { error = "群组不存在" });
            }

            // 检查是否已经是成员
            var isMember = _db.GroupMembers.Any(m => m.GroupId == groupId && m.UserId == me);
            if (isMember)
            {
                return BadRequest(new { error = "已经是群成员了" });
            }

            // 检查是否已有待处理的申请
            var existingRequest = _db.GroupJoinRequests
                .FirstOrDefault(r => r.GroupId == groupId && r.RequesterId == me && r.Status == "pending");
            if (existingRequest != null)
            {
                return BadRequest(new { error = "已发送加入申请，等待处理" });
            }

            // 创建加入申请
            var request = new GroupJoinRequest(groupId, me);
            _db.GroupJoinRequests.Add(request);
            await _db.SaveChangesAsync();

            return Ok(new { requestId = request.Id, message = "加入申请已发送" });
        }

        /// <summary>
        /// 获取群组的加入申请列表（仅owner/admin）
        /// </summary>
        [HttpGet("{groupId}/requests")]
        public IActionResult GetGroupJoinRequests(Guid groupId)
        {
            var me = GetCurrentUserId();
            var isOwner = _db.GroupMembers.Any(m => m.GroupId == groupId && m.UserId == me && m.Role == "owner");
            if (!isOwner)
            {
                return Forbid();
            }

            var requests = _db.GroupJoinRequests
                .Where(r => r.GroupId == groupId && r.Status == "pending")
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.Id, r.RequesterId, r.Status, r.CreatedAt })
                .ToList();
            return Ok(requests);
        }

        /// <summary>
        /// 获取我发送的群聊加入申请列表
        /// </summary>
        [HttpGet("requests/my")]
        public IActionResult GetMyJoinRequests()
        {
            var me = GetCurrentUserId();
            var requests = _db.GroupJoinRequests
                .Where(r => r.RequesterId == me)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.Id, r.GroupId, r.Status, r.CreatedAt, r.RespondedAt })
                .ToList();
            return Ok(requests);
        }

        /// <summary>
        /// 同意群聊加入申请（仅owner）
        /// </summary>
        [HttpPost("{groupId}/requests/{requestId}/accept")]
        public async Task<IActionResult> AcceptJoinRequest(Guid groupId, Guid requestId)
        {
            var me = GetCurrentUserId();
            var isOwner = _db.GroupMembers.Any(m => m.GroupId == groupId && m.UserId == me && m.Role == "owner");
            if (!isOwner)
            {
                return Forbid();
            }

            var request = _db.GroupJoinRequests.FirstOrDefault(r => r.Id == requestId && r.GroupId == groupId);
            if (request == null)
            {
                return NotFound(new { error = "加入申请不存在" });
            }

            if (request.Status != "pending")
            {
                return BadRequest(new { error = "该申请已处理" });
            }

            // 同意申请
            request.Accept();

            // 添加为群成员
            var exists = _db.GroupMembers.Any(m => m.GroupId == groupId && m.UserId == request.RequesterId);
            if (!exists)
            {
                _db.GroupMembers.Add(new GroupMember(groupId, request.RequesterId));
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "已同意加入申请" });
        }

        /// <summary>
        /// 拒绝群聊加入申请（仅owner）
        /// </summary>
        [HttpPost("{groupId}/requests/{requestId}/reject")]
        public async Task<IActionResult> RejectJoinRequest(Guid groupId, Guid requestId)
        {
            var me = GetCurrentUserId();
            var isOwner = _db.GroupMembers.Any(m => m.GroupId == groupId && m.UserId == me && m.Role == "owner");
            if (!isOwner)
            {
                return Forbid();
            }

            var request = _db.GroupJoinRequests.FirstOrDefault(r => r.Id == requestId && r.GroupId == groupId);
            if (request == null)
            {
                return NotFound(new { error = "加入申请不存在" });
            }

            if (request.Status != "pending")
            {
                return BadRequest(new { error = "该申请已处理" });
            }

            request.Reject();
            await _db.SaveChangesAsync();
            return Ok(new { message = "已拒绝加入申请" });
        }

        /// <summary>
        /// 向群组添加成员（仅owner，用于管理员直接添加）
        /// </summary>
        [HttpPost("{groupId}/members/{userId}")]
        public async Task<IActionResult> AddMember(Guid groupId, Guid userId)
        {
            var me = GetCurrentUserId();
            var isOwner = _db.GroupMembers.Any(m => m.GroupId == groupId && m.UserId == me && m.Role == "owner");
            if (!isOwner)
            {
                return Forbid();
            }
            var exists = _db.GroupMembers.Any(m => m.GroupId == groupId && m.UserId == userId);
            if (!exists)
            {
                _db.GroupMembers.Add(new GroupMember(groupId, userId));
                await _db.SaveChangesAsync();
            }
            return Ok();
        }

        /// <summary>
        /// 从群组移除成员（仅owner）
        /// </summary>
        [HttpDelete("{groupId}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(Guid groupId, Guid userId)
        {
            var me = GetCurrentUserId();
            var isOwner = _db.GroupMembers.Any(m => m.GroupId == groupId && m.UserId == me && m.Role == "owner");
            if (!isOwner)
            {
                return Forbid();
            }
            var list = _db.GroupMembers.Where(m => m.GroupId == groupId && m.UserId == userId);
            _db.GroupMembers.RemoveRange(list);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// 退出群聊（成员自助）
        /// </summary>
        [HttpDelete("{groupId}/leave")]
        public async Task<IActionResult> LeaveGroup(Guid groupId)
        {
            var me = GetCurrentUserId();
            var membership = await _db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == me);
            if (membership == null)
            {
                return NotFound(new { error = "您不是该群成员" });
            }

            if (string.Equals(membership.Role, "owner", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "群主无法直接退出，请先移交群主或解散群" });
            }

            _db.GroupMembers.Remove(membership);
            await _db.SaveChangesAsync();
            return Ok(new { message = "已退出群聊" });
        }

        /// <summary>
        /// 修改我在该群内的昵称（仅群内成员）
        /// </summary>
        [HttpPut("{groupId}/nickname")]
        public async Task<IActionResult> UpdateMyGroupNickname(Guid groupId, [FromBody] UpdateNicknameRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Nickname))
            {
                return BadRequest(new { error = "昵称不能为空" });
            }

            var me = GetCurrentUserId();
            var member = await _db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == me);
            if (member == null)
            {
                return Forbid();
            }

            member.Nickname = req.Nickname.Trim();
            await _db.SaveChangesAsync();
            return Ok(new { message = "昵称已更新" });
        }

        public class UpdateNicknameRequest
        {
            public string Nickname { get; set; } = string.Empty;
        }

        /// <summary>
        /// 通过群组名称模糊查询群组
        /// </summary>
        /// <param name="keyword">搜索关键词（群组名称）</param>
        /// <param name="pageIndex">页码，从0开始</param>
        /// <param name="pageSize">每页数量，默认20，最大100</param>
        /// <returns>群组列表（包含群组ID和群组名称）</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchGroupsByName(
            [FromQuery] string keyword,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 20)
        {
            // 验证参数
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { error = "搜索关键词不能为空" });
            }

            if (pageSize <= 0)
            {
                pageSize = 20;
            }
            else if (pageSize > 100)
            {
                pageSize = 100;
            }

            // 模糊查询群组名称（包含关键词）
            var query = _db.Groups
                .Where(g => g.Name.Contains(keyword))
                .OrderBy(g => g.Name)
                .Skip(pageIndex * pageSize)
                .Take(pageSize);

            var groups = await query
                .Select(g => new { g.Id, g.Name, g.OwnerId, g.CreatedAt })
                .ToListAsync();

            // 获取总数
            var totalCount = await _db.Groups
                .Where(g => g.Name.Contains(keyword))
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return Ok(new
            {
                data = groups,
                pageIndex,
                pageSize,
                totalCount,
                totalPages,
                hasPreviousPage = pageIndex > 0,
                hasNextPage = pageIndex < totalPages - 1
            });
        }

        public class CreateGroupRequest
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}


