namespace DreamSlave.Wecom.Services
{
    /// <summary>
    /// 企业微信机器人服务
    /// </summary>
    /// <remarks>
    /// https://developer.work.weixin.qq.com/document/path/99110
    /// </remarks>
    public class WecomBotService : IWecomBotService
    {
        private readonly ILogger<WecomBotService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _botName;
        private readonly string _botKey;
        private readonly string _botUrl;
        public WecomBotService(
            ILogger<WecomBotService> logger,
            IHttpClientFactory httpClientFactory,
            string botName,
            string botKey)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _botName = botName;
            _botKey = botKey;
            _botUrl = $"https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key={botKey}";
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private async Task<bool> SendRequestAsync(object obj)
        {
            using var client = _httpClientFactory.CreateClient("wecom_client");
            try
            {
                var json = JsonSerializer.Serialize(obj);
                using StringContent content = new(json, Encoding.UTF8, "application/json");
                using var resp = await client.PostAsync(_botUrl, content).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("[{Bot}] 发送失败: HTTP {Status} {Body}", _botName ?? "default", (int)resp.StatusCode, body);
                    return false;
                }

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    var errcode = root.TryGetProperty("errcode", out var ec) && ec.TryGetInt32(out var code) ? code : -1;
                    var errmsg = root.TryGetProperty("errmsg", out var em) ? em.GetString() : null;

                    if (errcode == 0)
                    {
                        return true;
                    }
                    _logger.LogError("[{Bot}] 发送失败: errcode={Errcode}, errmsg={Errmsg}", _botName ?? "default", errcode, errmsg);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{Bot}] 发送失败: 响应解析异常: {Body}", _botName ?? "default", body);
                    return false;
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "[{Bot}] 发送失败: 请求超时", _botName ?? "default");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Bot}] 发送失败: 未知错误", _botName ?? "default");
                return false;
            }
        }


        /// <summary>
        /// 发送普通文本消息
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<bool> SendTextMessageToBotAsync(string text)
        {
            var model = new
            {
                msgtype = "text",
                text = new
                {
                    content = text
                }
            };
            return await SendRequestAsync(model);
        }

        /// <summary>
        /// 发送md消息
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<bool> SendMarkDownMessageToBotAsync(string text)
        {
            var model = new
            {
                msgtype = "markdown",
                markdown = new
                {
                    content = text
                }
            };
            return await SendRequestAsync(model);
        }


        /// <summary>
        /// 发送mdv2消息
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<bool> SendMarkDownV2MessageToBotAsync(string text)
        {
            var model = new
            {
                msgtype = "markdown_v2",
                markdown_v2 = new
                {
                    content = text
                }
            };
            return await SendRequestAsync(model);
        }


        /// <summary>
        /// 发送图片给机器人
        /// </summary>
        /// <param name="fileBase64">文件base64编码</param>
        /// <param name="md5">图片内容（base64编码前）的md5值</param>
        /// <returns></returns>
        public async Task<bool> SendImageToBotAsync(string fileBase64, string md5)
        {
            var model = new
            {
                msgtype = "image",
                image = new
                {
                    base64 = fileBase64,
                    md5 = md5
                }
            };
            return await SendRequestAsync(model);
        }


        /// <summary>
        /// 发送文件给机器人
        /// </summary>
        /// <param name="mediaId"></param>
        /// <returns></returns>
        public async Task<bool> SendFileToBotAsync(string mediaId)
        {
            var model = new
            {
                msgtype = "file",
                file = new
                {
                    media_id = mediaId
                }
            };
            return await SendRequestAsync(model);
        }

        /// <summary>
        /// 发送图文消息
        /// </summary>
        /// <param name="articles"></param>
        /// <returns></returns>
        public async Task<bool> SendNewsToBotAsync(List<Article> articles)
        {
            var model = new
            {
                msgtype = "news",
                news = new
                {
                    articles = articles
                }
            };

            return await SendRequestAsync(model);
        }

        /// <summary>
        /// 通过 Webhook 上传文件，成功返回 media_id，失败返回空字符串
        /// </summary>
        /// <param name="fileBytes">文件二进制内容</param>
        /// <param name="fileName">文件名</param>
        public async Task<string> UploadFileToBotAsync(byte[] fileBytes, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "unknow";
            }

            var url = $"https://qyapi.weixin.qq.com/cgi-bin/webhook/upload_media?key={_botKey}&type=file";
            using var client = _httpClientFactory.CreateClient("wecom_client");

            try
            {
                using MultipartFormDataContent form = new();
                using ByteArrayContent fileContent = new(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                // name 必须为 media
                form.Add(fileContent, "media", fileName);

                using var resp = await client.PostAsync(url, form).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("[{Bot}] 上传失败: HTTP {Status} {Body}", _botName ?? "default", (int)resp.StatusCode, body);
                    return string.Empty;
                }

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    var errcode = root.TryGetProperty("errcode", out var ec) && ec.TryGetInt32(out var code) ? code : -1;
                    var errmsg = root.TryGetProperty("errmsg", out var em) ? em.GetString() : null;
                    var mediaId = root.TryGetProperty("media_id", out var mid) ? mid.GetString() : null;

                    if (errcode == 0 && !string.IsNullOrEmpty(mediaId))
                    {
                        return mediaId;
                    }

                    _logger.LogError("[{Bot}] 上传失败: errcode={Errcode}, errmsg={Errmsg}", _botName ?? "default", errcode, errmsg);
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{Bot}] 上传失败: 响应解析异常: {Body}", _botName ?? "default", body);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Bot}] 上传失败: 未知错误", _botName ?? "default");
                return string.Empty;
            }
        }
    }
}
