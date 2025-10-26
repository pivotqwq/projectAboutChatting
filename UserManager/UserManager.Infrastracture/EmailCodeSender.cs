using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserManager.Domain;

namespace UserManager.Infrastracture
{
    /// <summary>
    /// 邮箱验证码发送服务
    /// </summary>
    public class EmailCodeSender : IEmailCodeSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailCodeSender> _logger;

        public EmailCodeSender(IConfiguration configuration, ILogger<EmailCodeSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendCodeAsync(string email, string code)
        {
            try
            {
                // 从配置中读取SMTP设置
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.qq.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var senderEmail = _configuration["Email:SenderEmail"] ?? "2416101607@qq.com";
                var senderPassword = _configuration["Email:SenderPassword"] ?? "1357924680";
                var senderName = _configuration["Email:SenderName"] ?? "用户管理系统";

                // 创建邮件消息
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "验证码 - 用户管理系统",
                    Body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                <h2 style='color: #333;'>验证码注册/登录</h2>
                                <p>您的验证码是：</p>
                                <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; text-align: center;'>
                                    <span style='font-size: 32px; font-weight: bold; color: #007bff; letter-spacing: 5px;'>{code}</span>
                                </div>
                                <p style='color: #666; margin-top: 20px;'>验证码有效期为5分钟，请勿泄露给他人。</p>
                                <p style='color: #999; font-size: 12px; margin-top: 30px;'>如果这不是您的操作，请忽略此邮件。</p>
                            </div>
                        </body>
                        </html>
                    ",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                // 配置SMTP客户端
                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                // 发送邮件
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"验证码邮件已发送到: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送验证码邮件失败: {email}");
                Console.WriteLine($"[开发模拟] 向 {email} 发送验证码: {code}");
                
                // 抛出异常，让调用方知道发送失败
                throw new InvalidOperationException($"邮件发送失败: {ex.Message}。请检查SMTP配置是否正确。", ex);
            }
        }
    }
}

