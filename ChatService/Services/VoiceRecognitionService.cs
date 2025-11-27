using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace ChatService.Services
{
    /// <summary>
    /// 语音识别服务 - 使用 Azure Speech Service 将语音文件转换为文字
    /// </summary>
    public class VoiceRecognitionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VoiceRecognitionService> _logger;
        private readonly VoiceFileStorageService _fileStorage;

        public VoiceRecognitionService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<VoiceRecognitionService> logger,
            VoiceFileStorageService fileStorage)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _fileStorage = fileStorage;
        }

        /// <summary>
        /// 使用 Azure Speech Service 识别语音文件并转换为文字
        /// </summary>
        /// <param name="voiceFilePath">语音文件的相对路径</param>
        /// <param name="language">语言代码，默认 "zh-CN"</param>
        /// <returns>识别出的文字内容</returns>
        public async Task<string?> RecognizeAsync(string voiceFilePath, string language = "zh-CN")
        {
            var subscriptionKey = _configuration["VoiceRecognition:Azure:SubscriptionKey"];
            var region = _configuration["VoiceRecognition:Azure:Region"] ?? "eastasia";
            
            if (string.IsNullOrWhiteSpace(subscriptionKey))
            {
                _logger.LogWarning("Azure Speech Service 订阅密钥未配置，跳过语音识别");
                return null;
            }

            try
            {
                var filePath = _fileStorage.GetFilePath(voiceFilePath);
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("语音文件不存在: {FilePath}", filePath);
                    return null;
                }

                var audioBytes = await File.ReadAllBytesAsync(filePath);
                
                // Azure Speech Service API 端点
                var url = $"https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language={language}&format=detailed";
                
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);

                var content = new ByteArrayContent(audioBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(filePath));

                var response = await client.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AzureSpeechResponse>();
                    if (result?.RecognitionStatus == "Success" && !string.IsNullOrWhiteSpace(result.DisplayText))
                    {
                        _logger.LogInformation("语音识别成功: {Text}", result.DisplayText);
                        return result.DisplayText;
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Azure 语音识别API调用失败: {StatusCode}, {Error}", response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure 语音识别异常: {FilePath}", voiceFilePath);
            }

            return null;
        }

        /// <summary>
        /// 根据文件路径获取Content-Type
        /// </summary>
        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/mp4",
                ".ogg" => "audio/ogg",
                ".webm" => "audio/webm",
                _ => "audio/wav"
            };
        }

        private class AzureSpeechResponse
        {
            public string RecognitionStatus { get; set; } = string.Empty;
            public string DisplayText { get; set; } = string.Empty;
            public string Offset { get; set; } = string.Empty;
            public string Duration { get; set; } = string.Empty;
        }
    }
}

