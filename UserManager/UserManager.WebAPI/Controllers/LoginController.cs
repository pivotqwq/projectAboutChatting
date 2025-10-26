using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserManager.Domain;
using UserManager.Domain.ValueObjects;
using UserManager.Infrastracture;
using UserManager.WebAPI.Controllers.Requests;
using UserManager.WebAPI.Models;
using UserManager.WebAPI.Services;

namespace UserManager.WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly UserDomainService _userDomainService;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IUserRepository _userRepository;

        public LoginController(
            UserDomainService userDomainService, 
            JwtTokenService jwtTokenService,
            IUserRepository userRepository)
        {
            _userDomainService = userDomainService;
            _jwtTokenService = jwtTokenService;
            _userRepository = userRepository;
        }
        /// <summary>
        /// 账号密码登录
        /// </summary>
        [UnitOfWork(typeof(UserDBContext))]
        [HttpPost]
        public async Task<IActionResult> LoginByPhoneAndPassword
            (LoginByPhoneAndPwdRequest req)
        {
            if(req.Password.Length < 6)
            {
                return BadRequest("密码长度必须大于6");
            }
            
            // 验证至少输入了手机号或邮箱中的一个
            if(string.IsNullOrEmpty(req.UserBasic.PhoneNumber) && 
               string.IsNullOrEmpty(req.UserBasic.Email))
            {
                return BadRequest("请至少输入手机号或邮箱中的一个");
            }
            
            var result = await _userDomainService.CheckLoginAsync(req.UserBasic,req.Password);
            
            if(result == UserAccessResult.OK)
            {
                // 登录成功，生成JWT Token
                var user = await _userRepository.FindOneAsync(req.UserBasic);
                if(user == null)
                {
                    return BadRequest("用户不存在");
                }
                
                var userInfo = new Dictionary<string, string>();
                if(!string.IsNullOrEmpty(user.UserBasic.PhoneNumber))
                    userInfo["PhoneNumber"] = user.UserBasic.PhoneNumber;
                if(!string.IsNullOrEmpty(user.UserBasic.Email))
                    userInfo["Email"] = user.UserBasic.Email;
                
                var token = _jwtTokenService.GenerateToken(user.Id, user.Name, userInfo);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();
                
                var response = new LoginResponse
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = 3600, // 60分钟
                    UserId = user.Id,
                    UserName = user.Name
                };
                
                return Ok(response);
            }
            
            switch (result)
            {
                case UserAccessResult.OK:
                    return Ok("登录成功");
                case UserAccessResult.PhoneNumberNotFound:
                    return BadRequest("账号或者密码错误");
                case UserAccessResult.Lockout:
                    return BadRequest("用户被锁定，请稍后再试");
                case UserAccessResult.NoPassword:
                case UserAccessResult.PasswordError:
                    return BadRequest("账号或者密码错误");
                default:
                    throw new NotImplementedException("Unknown");
            }
        }
/*
        /// <summary>
        /// 发送验证码（手机短信）（不可用）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendVerificationCode(SendCodeRequest req)
        {
            if(string.IsNullOrEmpty(req.UserBasic.PhoneNumber))
            {
                return BadRequest("请输入手机号");
            }

            var result = await _userDomainService.SendCodeAsync(req.UserBasic);
            switch (result)
            {
                case UserAccessResult.OK:
                    return Ok("验证码已发送");
                case UserAccessResult.PhoneNumberNotFound:
                    return BadRequest("用户不存在");
                case UserAccessResult.Lockout:
                    return BadRequest("用户被锁定，请稍后再试");
                default:
                    throw new NotImplementedException("Unknown");
            }
        }

        /// <summary>
        /// 手机验证码登录(不可用)
        /// </summary>
        [UnitOfWork(typeof(UserDBContext))]
        [HttpPost]
        public async Task<IActionResult> LoginByPhoneAndCode(LoginByCodeRequest req)
        {
            if(string.IsNullOrEmpty(req.UserBasic.PhoneNumber))
            {
                return BadRequest("请输入手机号");
            }

            if(string.IsNullOrEmpty(req.Code))
            {
                return BadRequest("验证码不能为空");
            }

            var result = await _userDomainService.CheckCodeAsync(req.UserBasic, req.Code);
            
            if(result == CheckCodeResult.OK)
            {
                // 登录成功，生成JWT Token
                var user = await _userRepository.FindOneAsync(req.UserBasic);
                if(user == null)
                {
                    return BadRequest("用户不存在");
                }
                
                var userInfo = new Dictionary<string, string>();
                if(!string.IsNullOrEmpty(user.UserBasic.PhoneNumber))
                    userInfo["PhoneNumber"] = user.UserBasic.PhoneNumber;
                if(!string.IsNullOrEmpty(user.UserBasic.Email))
                    userInfo["Email"] = user.UserBasic.Email;
                
                var token = _jwtTokenService.GenerateToken(user.Id, user.Name, userInfo);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();
                
                var response = new LoginResponse
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = 3600,
                    UserId = user.Id,
                    UserName = user.Name
                };
                
                return Ok(response);
            }
            
            switch (result)
            {
                case CheckCodeResult.PhoneNumberNotFound:
                    return BadRequest("用户不存在");
                case CheckCodeResult.Lockout:
                    return BadRequest("用户被锁定，请稍后再试");
                case CheckCodeResult.CodeError:
                    return BadRequest("验证码错误或已过期");
                default:
                    throw new NotImplementedException("Unknown");
            }
        }
*/
        /// <summary>
        /// 发送邮箱验证码
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendEmailCode(SendEmailCodeRequest req)
        {

            if (string.IsNullOrEmpty(req.Email))
            {
                return BadRequest("邮箱不能为空");
            }

            try
            {
                var result = await _userDomainService.SendEmailCodeAsync(req.Email);
                switch (result)
                {
                    case UserAccessResult.OK:
                        return Ok("验证码已发送到您的邮箱");
                    case UserAccessResult.PhoneNumberNotFound:
                        return BadRequest("用户不存在");
                    case UserAccessResult.Lockout:
                        return BadRequest("用户被锁定，请稍后再试");
                    default:
                        throw new NotImplementedException("Unknown");
                }
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// 邮箱验证码登录
        /// </summary>
        [UnitOfWork(typeof(UserDBContext))]
        [HttpPost]
        public async Task<IActionResult> LoginByEmailCode(LoginByEmailCodeRequest req)
        {
            if(string.IsNullOrEmpty(req.Email))
            {
                return BadRequest("邮箱不能为空");
            }

            if(string.IsNullOrEmpty(req.Code))
            {
                return BadRequest("验证码不能为空");
            }

            var result = await _userDomainService.CheckEmailCodeAsync(req.Email, req.Code);
            
            if(result == CheckCodeResult.OK)
            {
                // 登录成功，生成JWT Token
                var userBasic = new UserBasic(null, req.Email);
                var user = await _userRepository.FindOneAsync(userBasic);
                if(user == null)
                {
                    return BadRequest("用户不存在");
                }
                
                var userInfo = new Dictionary<string, string>();
                if(!string.IsNullOrEmpty(user.UserBasic.PhoneNumber))
                    userInfo["PhoneNumber"] = user.UserBasic.PhoneNumber;
                if(!string.IsNullOrEmpty(user.UserBasic.Email))
                    userInfo["Email"] = user.UserBasic.Email;
                
                var token = _jwtTokenService.GenerateToken(user.Id, user.Name, userInfo);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();
                
                var response = new LoginResponse
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = 3600,
                    UserId = user.Id,
                    UserName = user.Name
                };
                
                return Ok(response);
            }
            
            switch (result)
            {
                case CheckCodeResult.OK:
                    return Ok("登录成功");
                case CheckCodeResult.PhoneNumberNotFound:
                    return BadRequest("用户不存在");
                case CheckCodeResult.Lockout:
                    return BadRequest("用户被锁定，请稍后再试");
                case CheckCodeResult.CodeError:
                    return BadRequest("验证码错误或已过期");
                default:
                    throw new NotImplementedException("Unknown");
            }
        }

        /// <summary>
        /// 发送注册邮箱验证码
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendRegisterEmailCode(SendRegisterEmailCodeRequest req)
        {
            if (string.IsNullOrEmpty(req.Email))
            {
                return BadRequest("邮箱不能为空");
            }

            // 检查邮箱是否已被注册
            var userBasic = new UserBasic(null, req.Email);
            var existingUser = await _userRepository.FindOneAsync(userBasic);
            if (existingUser != null)
            {
                return BadRequest("该邮箱已被注册");
            }

            try
            {
                await _userDomainService.SendRegisterEmailCodeAsync(req.Email);
                return Ok("验证码已发送到您的邮箱");
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }

    /// <summary>
    /// 发送注册邮箱验证码请求
    /// </summary>
    public class SendRegisterEmailCodeRequest
    {
        public string Email { get; set; } = string.Empty;
    }

}
