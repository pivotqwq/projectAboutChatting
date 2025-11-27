using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace ChatService.Services
{
    /// <summary>
    /// 语音文件存储服务 - 管理语音文件的上传、存储和访问
    /// </summary>
    public class VoiceFileStorageService
    {
        private readonly string _basePath;
        private readonly ILogger<VoiceFileStorageService> _logger;
        private readonly IWebHostEnvironment _environment;

        public VoiceFileStorageService(IConfiguration configuration, ILogger<VoiceFileStorageService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            
            try
            {
                // 从配置读取存储路径，默认为 wwwroot/voices
                var configuredPath = configuration["VoiceStorage:Path"];
                if (!string.IsNullOrWhiteSpace(configuredPath))
                {
                    _basePath = configuredPath;
                }
                else
                {
                    var contentRootPath = _environment?.ContentRootPath ?? AppDomain.CurrentDomain.BaseDirectory;
                    _basePath = Path.Combine(contentRootPath, "wwwroot", "voices");
                }
                
                // 确保目录存在
                if (!Directory.Exists(_basePath))
                {
                    Directory.CreateDirectory(_basePath);
                    _logger.LogInformation("创建语音文件存储目录: {BasePath}", _basePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化语音文件存储服务失败");
                // 使用临时目录作为后备方案
                _basePath = Path.Combine(Path.GetTempPath(), "ChatService", "voices");
                try
                {
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                    }
                }
                catch
                {
                    // 如果仍然失败，记录错误但继续运行
                    _logger.LogCritical("无法创建语音文件存储目录: {BasePath}", _basePath);
                }
            }
        }

        /// <summary>
        /// 保存语音文件
        /// </summary>
        /// <param name="fileStream">文件流</param>
        /// <param name="fileName">文件名（包含扩展名）</param>
        /// <returns>存储的相对路径</returns>
        public async Task<string> SaveVoiceFileAsync(Stream fileStream, string fileName)
        {
            try
            {
                // 生成唯一文件名：时间戳 + GUID + 原扩展名
                var extension = Path.GetExtension(fileName);
                var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(_basePath, uniqueFileName);

                using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(file);
                }

                // 返回相对于wwwroot的路径，用于URL访问
                var relativePath = $"voices/{uniqueFileName}";
                _logger.LogInformation("语音文件已保存: {FilePath}", relativePath);
                
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存语音文件失败: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// 获取语音文件的完整路径
        /// </summary>
        /// <param name="relativePath">相对路径（如 voices/xxx.wav）</param>
        /// <returns>完整文件路径</returns>
        public string GetFilePath(string relativePath)
        {
            // 如果已经是完整路径，直接返回
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            // 移除前导斜杠和 voices/ 前缀
            var cleanPath = relativePath.TrimStart('/', '\\');
            if (cleanPath.StartsWith("voices/", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(7);
            }

            return Path.Combine(_basePath, cleanPath);
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <returns>是否存在</returns>
        public bool FileExists(string relativePath)
        {
            var filePath = GetFilePath(relativePath);
            return File.Exists(filePath);
        }

        /// <summary>
        /// 删除语音文件
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteFile(string relativePath)
        {
            try
            {
                var filePath = GetFilePath(relativePath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("语音文件已删除: {FilePath}", relativePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除语音文件失败: {RelativePath}", relativePath);
                return false;
            }
        }

        /// <summary>
        /// 获取语音文件的访问URL
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <param name="baseUrl">服务器基础URL（如 http://localhost:9293）</param>
        /// <returns>完整URL</returns>
        public string GetFileUrl(string relativePath, string baseUrl)
        {
            var cleanPath = relativePath.TrimStart('/', '\\');
            if (!cleanPath.StartsWith("voices/", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = "voices/" + cleanPath;
            }
            
            var url = $"{baseUrl.TrimEnd('/')}/{cleanPath.Replace('\\', '/')}";
            return url;
        }
    }
}

