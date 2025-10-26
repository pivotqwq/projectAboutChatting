using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace UserManager.WebAPI.Services
{
    /// <summary>
    /// JWT Token服务
    /// </summary>
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 生成JWT Token
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="userName">用户名</param>
        /// <param name="userBasicInfo">用户基本信息（可选）</param>
        /// <param name="role">用户角色（可选）</param>
        /// <returns>JWT Token</returns>
        public string GenerateToken(Guid userId, string userName, Dictionary<string, string>? userBasicInfo = null, string? role = null)
        {
            var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey未配置");
            var issuer = _configuration["Jwt:Issuer"] ?? "UserManagerAPI";
            var audience = _configuration["Jwt:Audience"] ?? "UserManagerClient";
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "60");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // 添加额外的用户信息
            if (userBasicInfo != null)
            {
                foreach (var kvp in userBasicInfo)
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value));
                }
            }

            // 添加角色信息
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// 生成刷新Token
        /// </summary>
        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}

