using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UserManager.Infrastracture;
using UserManager.Domain.ValueObjects;
using UserManager.Domain.Entities;
using UserManager.WebAPI.Controllers.Requests;
using UserManager.WebAPI;
using UserManager.Domain;
using System.Security.Claims;


namespace UserManager.WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [UnitOfWork(typeof(UserDBContext))]
    public class UsersMgrController : ControllerBase
    {
        private readonly UserDBContext dbCtx;
        private readonly UserDomainService domainService;
        private readonly IUserRepository repository;
        private readonly IUserProfileRepository userProfileRepository;

        public UsersMgrController(UserDBContext dbCtx, UserDomainService domainService, IUserRepository repository, IUserProfileRepository userProfileRepository)
        {
            this.dbCtx = dbCtx;
            this.domainService = domainService;
            this.repository = repository;
            this.userProfileRepository = userProfileRepository;
        }

        [HttpPost]
        public async Task<IActionResult> AddNew(RegisterRequest req)
        {
            // 验证输入了手机号或邮箱
            if(string.IsNullOrEmpty(req.UserBasic.PhoneNumber))
            {
                return BadRequest("请输入手机号");
            }

            if (string.IsNullOrEmpty(req.UserBasic.Email))
            {
                return BadRequest("请输入邮箱");
            }

            // 验证邮箱验证码
            if (string.IsNullOrEmpty(req.EmailCode))
            {
                return BadRequest("请输入邮箱验证码");
            }

            // 检查邮箱验证码是否正确
            bool isCodeValid = await domainService.CheckRegisterEmailCodeAsync(req.UserBasic.Email, req.EmailCode);
            if (!isCodeValid)
            {
                return BadRequest("邮箱验证码错误或已过期");
            }

            if ((await repository.FindOneAsync(req.UserBasic))!=null)
            {
                return BadRequest("该手机号或邮箱已经被注册");
            }
            
            User user = new User(req.UserBasic);
            // 设置用户密码
            user.ChangePassword(req.Password);
            
            dbCtx.Users.Add(user);
            
            // 自动创建用户个人信息
            var userProfile = new UserProfile(user.Id);
            userProfile.RealName = user.Name;
            await userProfileRepository.AddAsync(userProfile);
            
            return Ok("注册成功");
        }

        [HttpPut]
        public async Task<IActionResult> AdminChangePassword(ChangePasswordRequest req)
        {
            var user = await repository.FindOneAsync(req.Id);
            if(user == null)
            {
                return NotFound();
            }
            user.ChangePassword(req.Password);
            return Ok("成功");
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Unlock(Guid id)
        {
            var user = await repository.FindOneAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            domainService.ResetAccessFail(user);
            return Ok("成功");
        }

        /// <summary>
        /// 锁定用户（管理员接口）
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> LockUser([FromBody] LockUserRequest req)
        {
            var user = await repository.FindOneAsync(req.UserId);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            // 设置锁定时间
            if (req.LockMinutes > 0)
            {
                // 临时锁定
                user.UserAccessFail.LockUntil(DateTime.UtcNow.AddMinutes(req.LockMinutes));
            }
            else
            {
                // 永久锁定
                user.UserAccessFail.LockPermanently();
            }

            return Ok($"用户已锁定{(req.LockMinutes > 0 ? $" {req.LockMinutes} 分钟" : "（永久锁定）")}");
        }

        [HttpGet]
        [Authorize] // 需要JWT认证
        public async Task<IActionResult> GetAll()
        {
            var users = await dbCtx.Users.ToListAsync();
            var userNames = users.Select(u => u.Name).ToList();
            return Ok(userNames);
        }

        /// <summary>
        /// 获取当前登录用户信息
        /// </summary>
        [HttpGet]
        [Authorize] // 需要JWT认证
        public async Task<IActionResult> GetCurrentUser()
        {
            // 从JWT Token中获取用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized("无效的Token");
            }

            var user = await repository.FindOneAsync(userId);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            // 只返回用户名
            return Ok(user.Name);
        }

        /// <summary>
        /// 通过用户ID获取用户完整信息
        /// </summary>
        [HttpGet("{userId}")]
        [Authorize] // 需要JWT认证
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            var user = await repository.FindOneAsync(userId);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            // 获取用户个人资料
            var userProfile = await userProfileRepository.GetByUserIdAsync(userId);

            var response = new Models.UserInfoResponse
            {
                UserId = user.Id,
                UserName = user.Name,
                UserBasic = new Models.UserBasicDto
                {
                    PhoneNumber = user.UserBasic.PhoneNumber,
                    Email = user.UserBasic.Email
                },
                UserProfile = userProfile != null ? new Models.UserProfileDto
                {
                    Avatar = userProfile.Avatar,
                    RealName = userProfile.RealName,
                    Gender = userProfile.Gender,
                    Birthday = userProfile.Birthday,
                    Region = userProfile.Region,
                    PhoneNumber = userProfile.PhoneNumber,
                    Bio = userProfile.Bio
                } : null
            };

            return Ok(response);
        }

        /// <summary>
        /// 通过邮箱验证码修改密码（无需认证）
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> ChangePasswordByEmail([FromBody] ChangePasswordByEmailRequest req)
        {
            // 验证输入参数
            if (string.IsNullOrEmpty(req.Email))
            {
                return BadRequest("请输入邮箱地址");
            }

            if (string.IsNullOrEmpty(req.EmailCode))
            {
                return BadRequest("请输入邮箱验证码");
            }

            if (string.IsNullOrEmpty(req.NewPassword))
            {
                return BadRequest("请输入新密码");
            }

            // 验证邮箱验证码
            var checkResult = await domainService.CheckEmailCodeAsync(req.Email, req.EmailCode);
            if (checkResult != CheckCodeResult.OK)
            {
                switch (checkResult)
                {
                    case CheckCodeResult.PhoneNumberNotFound:
                        return BadRequest("用户不存在");
                    case CheckCodeResult.Lockout:
                        return BadRequest("用户被锁定，请稍后再试");
                    case CheckCodeResult.CodeError:
                        return BadRequest("验证码错误或已过期");
                    default:
                        return BadRequest("验证失败");
                }
            }

            // 根据邮箱查找用户
            var userBasic = new UserBasic(null, req.Email);
            var user = await repository.FindOneAsync(userBasic);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            // 修改密码
            user.ChangePassword(req.NewPassword);
            return Ok("密码修改成功");
        }

        /// <summary>
        /// 获取当前用户个人信息
        /// </summary>
        [HttpGet]
        [Authorize] // 需要JWT认证
        public async Task<IActionResult> GetMyProfile()
        {
            // 从JWT Token中获取用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized("无效的Token");
            }

            var profile = await userProfileRepository.GetByUserIdAsync(userId);
            
            if (profile == null)
            {
                var userProfile = new UserProfile(userId);
                await userProfileRepository.AddAsync(userProfile);
                return Ok(userProfile);
            }

            // 返回个人信息
            var profileInfo = new
            {
                profile.Id,
                profile.UserId,
                profile.Avatar,
                profile.RealName,
                profile.Gender,
                profile.Birthday,
                profile.Region,
                profile.PhoneNumber,
                profile.Bio,
                profile.CreatedAt,
                profile.UpdatedAt
            };

            return Ok(profileInfo);
        }

        /// <summary>
        /// 更新当前用户个人信息
        /// </summary>
        [HttpPut]
        [Authorize] // 需要JWT认证
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest req)
        {
            // 从JWT Token中获取用户ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized("无效的Token");
            }
            var user = await repository.FindOneAsync(Guid.Parse(userIdClaim));
            user.Name = (req.RealName == null ? user.Name : req.RealName);

            var profile = await userProfileRepository.GetByUserIdAsync(userId);
            if (profile == null)
            {
                var userProfile = new UserProfile(userId);
                userProfile.UpdateProfile(
                req.Avatar,
                req.RealName,
                req.Gender,
                req.Birthday,
                req.Region,
                req.PhoneNumber,
                req.Bio
                );
                await userProfileRepository.AddAsync(userProfile);
                return Ok(userProfile);
            }

            // 更新个人信息
            profile.UpdateProfile(
                req.Avatar,
                req.RealName,
                req.Gender,
                req.Birthday,
                req.Region,
                req.PhoneNumber,
                req.Bio
            );

            await userProfileRepository.UpdateAsync(profile);
            return Ok("个人信息更新成功");
        }

        /// <summary>
        /// 通过用户名模糊查询用户
        /// </summary>
        /// <param name="keyword">搜索关键词（用户名）</param>
        /// <param name="pageIndex">页码，从0开始</param>
        /// <param name="pageSize">每页数量，默认20，最大100</param>
        /// <returns>用户列表（包含用户ID和用户名）</returns>
        [HttpGet("search")]
        [Authorize] // 需要JWT认证
        public async Task<IActionResult> SearchUsersByName(
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

            // 模糊查询用户名（包含关键词）
            var query = dbCtx.Users
                .Where(u => u.Name.Contains(keyword))
                .OrderBy(u => u.Name)
                .Skip(pageIndex * pageSize)
                .Take(pageSize);

            var users = await query
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();

            // 获取总数
            var totalCount = await dbCtx.Users
                .Where(u => u.Name.Contains(keyword))
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return Ok(new
            {
                data = users,
                pageIndex,
                pageSize,
                totalCount,
                totalPages,
                hasPreviousPage = pageIndex > 0,
                hasNextPage = pageIndex < totalPages - 1
            });
        }
    }

}

