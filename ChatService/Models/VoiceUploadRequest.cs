using Microsoft.AspNetCore.Http;

namespace ChatService.Models
{
    /// <summary>
    /// 语音文件上传请求模型
    /// </summary>
    public class VoiceUploadRequest
    {
        /// <summary>
        /// 语音文件
        /// </summary>
        public IFormFile File { get; set; } = null!;
    }

    /// <summary>
    /// 语音识别请求模型
    /// </summary>
    public class VoiceRecognizeRequest
    {
        /// <summary>
        /// 语音文件的相对路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 识别语言代码，默认 "zh-CN"
        /// </summary>
        public string Language { get; set; } = "zh-CN";
    }
}

