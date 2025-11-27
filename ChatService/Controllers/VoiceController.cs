using ChatService.Services;
using ChatService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatService.Controllers
{
    /// <summary>
    /// 语音消息管理接口 - 提供语音文件上传功能
    /// </summary>
    [ApiController]
    [Route("api/voice")]
    [Authorize]
    [Produces("application/json")]
    public class VoiceController : ControllerBase
    {
        private readonly VoiceFileStorageService _fileStorage;
        // private readonly VoiceRecognitionService _recognitionService;  // 暂时屏蔽语音识别功能
        private readonly ILogger<VoiceController> _logger;
        private readonly IConfiguration _configuration;

        public VoiceController(
            VoiceFileStorageService fileStorage,
            // VoiceRecognitionService recognitionService,  // 暂时屏蔽语音识别功能
            ILogger<VoiceController> logger,
            IConfiguration configuration)
        {
            _fileStorage = fileStorage;
            // _recognitionService = recognitionService;  // 暂时屏蔽语音识别功能
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 从JWT Claims中获取当前用户ID
        /// </summary>
        private string GetCurrentUserId()
        {
            return User?.FindFirstValue("sub")
                   ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User?.Identity?.Name
                   ?? string.Empty;
        }

        /// <summary>
        /// 上传语音文件
        /// </summary>
        /// <param name="request">上传请求，包含语音文件</param>
        /// <returns>上传结果，包含文件路径和访问URL</returns>
        /// <response code="200">上传成功</response>
        /// <response code="400">文件格式不支持或文件过大</response>
        /// <response code="401">未授权访问</response>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadVoice([FromForm] VoiceUploadRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new { error = "无效的认证信息" });
                }

                if (request == null || request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new { error = "请选择要上传的语音文件" });
                }

                var file = request.File;

                // 验证文件大小（最大10MB）
                var maxSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxSize)
                {
                    return BadRequest(new { error = "语音文件大小不能超过10MB" });
                }

                // 验证文件格式
                var allowedExtensions = new[] { ".wav", ".mp3", ".m4a", ".ogg", ".webm" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { error = $"不支持的文件格式。支持的格式: {string.Join(", ", allowedExtensions)}" });
                }

                // 保存文件
                string relativePath;
                using (var stream = file.OpenReadStream())
                {
                    relativePath = await _fileStorage.SaveVoiceFileAsync(stream, file.FileName);
                }

                // 获取文件访问URL
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var fileUrl = _fileStorage.GetFileUrl(relativePath, baseUrl);

                var result = new
                {
                    filePath = relativePath,
                    fileUrl = fileUrl,
                    fileName = file.FileName,
                    fileSize = file.Length,
                    uploadedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传语音文件失败");
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }

        // 暂时屏蔽语音识别功能
        /*
        /// <summary>
        /// 对已上传的语音文件进行识别
        /// </summary>
        /// <param name="request">识别请求，包含文件路径和语言代码</param>
        /// <returns>识别结果</returns>
        /// <response code="200">识别成功</response>
        /// <response code="404">文件不存在</response>
        [HttpPost("recognize")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RecognizeVoice([FromForm] VoiceRecognizeRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.FilePath))
                {
                    return BadRequest(new { error = "文件路径不能为空" });
                }

                if (!_fileStorage.FileExists(request.FilePath))
                {
                    return NotFound(new { error = "语音文件不存在" });
                }

                var recognizedText = await _recognitionService.RecognizeAsync(request.FilePath, request.Language);
                
                return Ok(new
                {
                    filePath = request.FilePath,
                    recognizedText = recognizedText ?? string.Empty,
                    recognitionStatus = recognizedText != null ? "success" : "failed",
                    language = request.Language
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "语音识别失败: {FilePath}", request?.FilePath);
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }
        */

        /// <summary>
        /// 删除语音文件
        /// </summary>
        /// <param name="filePath">语音文件的相对路径</param>
        /// <returns>删除结果</returns>
        /// <response code="200">删除成功</response>
        /// <response code="404">文件不存在</response>
        [HttpDelete("{filePath}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteVoice([FromRoute] string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return BadRequest(new { error = "文件路径不能为空" });
                }

                if (!_fileStorage.FileExists(filePath))
                {
                    return NotFound(new { error = "语音文件不存在" });
                }

                var deleted = _fileStorage.DeleteFile(filePath);
                if (deleted)
                {
                    return Ok(new { message = "文件已删除", filePath });
                }
                else
                {
                    return StatusCode(500, new { error = "删除文件失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除语音文件失败: {FilePath}", filePath);
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }
    }
}

