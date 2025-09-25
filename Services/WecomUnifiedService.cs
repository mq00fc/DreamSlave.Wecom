namespace DreamSlave.Wecom.Services
{
    internal sealed class WecomUnifiedService : IWecomUnifiedService
    {
        private static readonly ConcurrentDictionary<string, Models.Config> _configs = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<WecomUnifiedService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        public WecomUnifiedService(
            ILogger<WecomUnifiedService> logger,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        // 缓存键构造
        private static string TokenKey(string serviceName) => $"wecom:{serviceName}:access_token";
        private static string TicketKey(string serviceName) => $"wecom:{serviceName}:jsapi_ticket";

        #region 配置 & 注册
        /// <summary>
        /// 注册（或覆盖）企业微信实例配置
        /// </summary>
        public static void RegisterConfig(string serviceName, Models.Config cfg)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) serviceName = "default";
            _configs.AddOrUpdate(serviceName, _ => cfg, (_, __) => cfg);
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        private Models.Config? GetCfg(string serviceName) => _configs.TryGetValue(serviceName, out var c) ? c : null;
        public Models.Config? GetConfig(string serviceName = "default") => GetCfg(serviceName);
        #endregion

        #region Token / Ticket
        /// <summary>
        /// 获取未过期的 AccessToken（可能为 null）
        /// </summary>
        public string? GetToken(string serviceName = "default")
            => _cache.TryGetValue(TokenKey(serviceName), out string? v) ? v : null;

        /// <summary>
        /// 获取未过期的 jsapi_ticket（可能为 null）
        /// </summary>
        public string? GetTicket(string serviceName = "default")
            => _cache.TryGetValue(TicketKey(serviceName), out string? v) ? v : null;

        /// <summary>
        /// 强制刷新 AccessToken 并写入缓存
        /// </summary>
        public async Task<WecomAccessToken?> RefreshAccessTokenAsync(string serviceName = "default")
        {
            var cfg = GetCfg(serviceName);
            if (cfg == null) return null;
            using var client = _httpClientFactory.CreateClient("wecom_client");
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={cfg.CorpID}&corpsecret={cfg.CorpSecret}";
            using var resp = await client.GetAsync(url);
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("[unified:{Name}] 获取AccessToken失败 {Status}\n{Content}", serviceName, resp.StatusCode, text);
                return null;
            }
            var model = JsonSerializer.Deserialize<WecomAccessToken>(text);
            if (model?.Errcode != 0 && cfg.EnableLog)
            {
                _logger.LogError("[unified:{Name}] 获取access_token失败:{Content}", serviceName, text);
            }
            if (!string.IsNullOrEmpty(model?.AccessToken))
            {
                var ttl = TimeSpan.FromSeconds(Math.Max(model.ExpiresIn - 60, 60)); // 预留 1 分钟
                _cache.Set(TokenKey(serviceName), model.AccessToken, ttl);
            }
            return model;
        }

        /// <summary>
        /// 强制刷新 jsapi_ticket 并写入缓存
        /// </summary>
        public async Task<WecomJsapiTicket?> RefreshJsApiTicketAsync(string serviceName = "default")
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return null;
            var token = GetToken(serviceName); if (string.IsNullOrEmpty(token)) return null;
            using var client = _httpClientFactory.CreateClient("wecom_client");
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/get_jsapi_ticket?access_token={token}";
            using var resp = await client.GetAsync(url);
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("[unified:{Name}] 获取jsapi-ticket失败 {Status}\n{Content}", serviceName, resp.StatusCode, text);
                return null;
            }
            var model = JsonSerializer.Deserialize<WecomJsapiTicket>(text);
            if (model?.Errcode != 0 && cfg.EnableLog)
                _logger.LogError("[unified:{Name}] 获取jsapi-ticket失败:{Content}", serviceName, text);
            if (!string.IsNullOrEmpty(model?.Ticket))
            {
                var ttl = TimeSpan.FromSeconds(Math.Max(model.ExpiresIn - 60, 60));
                _cache.Set(TicketKey(serviceName), model.Ticket, ttl);
            }
            return model;
        }

        /// <summary>
        /// 构造前端 js-sdk 所需签名 DTO
        /// </summary>
        public JsapiTicketDto? GetJsapiTicketDto(string serviceName, string url)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return null;
            if (cfg.AgentId < 1)
            {
                _logger.LogError("[unified:{Name}] 获取JsapiTicketDto失败:AgentId未配置", serviceName);
                return null;
            }
            var ticket = GetTicket(serviceName);
            if (string.IsNullOrEmpty(ticket))
            {
                _logger.LogError("[unified:{Name}] 获取JsapiTicketDto失败:ticket为空", serviceName);
                return null;
            }
            var sharpIdx = url.IndexOf('#');
            if (sharpIdx >= 0) url = url[..sharpIdx];
            var dto = new JsapiTicketDto
            {
                CorpId = cfg.CorpID,
                AgentId = cfg.AgentId,
                Ticket = ticket,
                Nonce = Guid.NewGuid().ToString("N"),
                TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            };
            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                {"jsapi_ticket", dto.Ticket},
                {"noncestr", dto.Nonce},
                {"timestamp", dto.TimeStamp},
                {"url", url}
            };
            var sb = new StringBuilder(128);
            foreach (var kv in parameters) sb.Append(kv.Key).Append('=').Append(kv.Value).Append('&');
            var raw = WebUtility.UrlDecode(sb.ToString().TrimEnd('&'));
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(raw));
            var sigBuilder = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sigBuilder.Append(b.ToString("x2"));
            dto.Signature = sigBuilder.ToString();
            return dto;
        }
        #endregion

        #region OAuth / WebLogin
        public string BuildOAuth2Url(string serviceName, string url, string state)
        {
            var cfg = GetCfg(serviceName) ?? throw new InvalidOperationException($"wecom config '{serviceName}' not found");
            var sb = new StringBuilder(256);
            sb.Append("https://open.weixin.qq.com/connect/oauth2/authorize?")
              .Append("appid=").Append(cfg.CorpID)
              .Append("&redirect_uri=").Append(WebUtility.UrlEncode(url))
              .Append("&response_type=code&scope=snsapi_privateinfo&state=").Append(state)
              .Append("&agentid=").Append(cfg.AgentId)
              .Append("#wechat_redirect");
            return sb.ToString();
        }
        public string BuildWebLoginUrl(string serviceName, string url, string state)
        {
            var cfg = GetCfg(serviceName) ?? throw new InvalidOperationException($"wecom config '{serviceName}' not found");
            var sb = new StringBuilder(256);
            sb.Append("https://login.work.weixin.qq.com/wwlogin/sso/login?login_type=CorpApp&appid=")
              .Append(cfg.CorpID)
              .Append("&agentid=").Append(cfg.AgentId)
              .Append("&redirect_uri=").Append(WebUtility.UrlEncode(url))
              .Append("&state=").Append(state)
              .Append("#wechat_redirect");
            return sb.ToString();
        }
        public WebLoginDto BuildWebLoginDto(string serviceName, string url, string state)
        {
            var cfg = GetCfg(serviceName) ?? throw new InvalidOperationException($"wecom config '{serviceName}' not found");
            return new WebLoginDto
            {
                AgentId = cfg.AgentId,
                AppId = cfg.CorpID,
                LoginType = "CorpApp",
                Lang = "zh",
                RedirectUri = url,
                State = state
            };
        }
        public async Task<OAuth2UserInfo?> GetOAuth2UserInfoAsync(string serviceName, string code)
        {
            var token = GetToken(serviceName); if (string.IsNullOrEmpty(token)) return null;
            using var client = _httpClientFactory.CreateClient("wecom_client");
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/auth/getuserinfo?access_token={token}&code={code}";
            using var resp = await client.GetAsync(url);
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<OAuth2UserInfo>(text);
        }
        public async Task<OAuth2UserDetail?> GetOAuth2UserDetailAsync(string serviceName, OAuth2UserInfo info)
        {
            var token = GetToken(serviceName); if (string.IsNullOrEmpty(token)) return null;
            using var client = _httpClientFactory.CreateClient("wecom_client");
            var payload = new { user_ticket = info.UserTicket };
            var json = JsonSerializer.Serialize(payload);
            using var resp = await client.PostAsync($"https://qyapi.weixin.qq.com/cgi-bin/auth/getuserdetail?access_token={token}", new StringContent(json, Encoding.UTF8, "application/json"));
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<OAuth2UserDetail>(text);
        }
        public async Task<WebLoginUserInfo?> GetWebLoginUserInfoAsync(string serviceName, string code)
        {
            var token = GetToken(serviceName); if (string.IsNullOrEmpty(token)) return null;
            using var client = _httpClientFactory.CreateClient("wecom_client");
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/auth/getuserinfo?access_token={token}&code={code}";
            using var resp = await client.GetAsync(url);
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<WebLoginUserInfo>(text);
        }
        #endregion

        #region 消息发送
        private async Task<SendMessageResponse?> PostMessageAsync(string serviceName, object dto)
        {
            var token = GetToken(serviceName);
            if (string.IsNullOrEmpty(token)) return new SendMessageResponse { Errcode = -1, Errmsg = "token empty" };
            using var client = _httpClientFactory.CreateClient("wecom_client");
            var body = JsonSerializer.Serialize(dto, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync($"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={token}", content);
            var text = await resp.Content.ReadAsStringAsync();
            var model = JsonSerializer.Deserialize<SendMessageResponse>(text);
            if (model?.Errcode != 0 && GetCfg(serviceName)?.EnableLog == true)
                _logger.LogError("[unified:{Name}] 发送消息失败:{Body}", serviceName, text);
            return model;
        }
        public Task<SendMessageResponse?> SendTextMessageAsync(string serviceName, string content, params string[] userIds)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return Task.FromResult<SendMessageResponse?>(null);
            var model = new SendMessageText
            {
                Agentid = cfg.AgentId,
                Touser = string.Join("|", userIds),
                Toparty = string.Empty,
                Totag = string.Empty,
                Text = new MessageContent { Content = content }
            };
            return PostMessageAsync(serviceName, model);
        }
        public Task<SendMessageResponse?> SendMarkDownMessageAsync(string serviceName, string content, params string[] userIds)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return Task.FromResult<SendMessageResponse?>(null);
            var model = new SendMessageMD
            {
                Agentid = cfg.AgentId,
                Touser = string.Join("|", userIds),
                Toparty = string.Empty,
                Totag = string.Empty,
                Markdown = new MessageContent { Content = content }
            };
            return PostMessageAsync(serviceName, model);
        }
        public Task<SendMessageResponse?> SendTextMessageAsync(string serviceName, MessageBase meta, MessageContent content)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return Task.FromResult<SendMessageResponse?>(null);
            var model = new SendMessageText { Agentid = cfg.AgentId, Touser = meta.Touser, Toparty = meta.Toparty, Totag = meta.Totag, Text = content, EnableDuplicateCheck = meta.EnableDuplicateCheck, Safe = meta.Safe, EnableIdTrans = meta.EnableIdTrans, DuplicateCheckInterval = meta.DuplicateCheckInterval };
            return PostMessageAsync(serviceName, model);
        }
        public Task<SendMessageResponse?> SendMarkDownMessageAsync(string serviceName, MessageBase meta, MessageContent content)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return Task.FromResult<SendMessageResponse?>(null);
            var model = new SendMessageMD { Agentid = cfg.AgentId, Touser = meta.Touser, Toparty = meta.Toparty, Totag = meta.Totag, Markdown = content, EnableDuplicateCheck = meta.EnableDuplicateCheck, Safe = meta.Safe, EnableIdTrans = meta.EnableIdTrans, DuplicateCheckInterval = meta.DuplicateCheckInterval };
            return PostMessageAsync(serviceName, model);
        }
        public Task<SendMessageResponse?> SendImageMessageAsync(string serviceName, MessageBase meta, MessageFile file)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return Task.FromResult<SendMessageResponse?>(null);
            var model = new SendMessageImage { Agentid = cfg.AgentId, Touser = meta.Touser, Toparty = meta.Toparty, Totag = meta.Totag, Image = file, EnableDuplicateCheck = meta.EnableDuplicateCheck, Safe = meta.Safe, EnableIdTrans = meta.EnableIdTrans, DuplicateCheckInterval = meta.DuplicateCheckInterval };
            return PostMessageAsync(serviceName, model);
        }
        public Task<SendMessageResponse?> SendVoiceMessageAsync(string serviceName, MessageBase meta, MessageFile file)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return Task.FromResult<SendMessageResponse?>(null);
            var model = new SendMessageVoice { Agentid = cfg.AgentId, Touser = meta.Touser, Toparty = meta.Toparty, Totag = meta.Totag, Voice = file, EnableDuplicateCheck = meta.EnableDuplicateCheck, Safe = meta.Safe, EnableIdTrans = meta.EnableIdTrans, DuplicateCheckInterval = meta.DuplicateCheckInterval };
            return PostMessageAsync(serviceName, model);
        }
        public Task<SendMessageResponse?> SendVideoMessageAsync(string serviceName, MessageBase meta, MessageVideo video)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return Task.FromResult<SendMessageResponse?>(null);
            var model = new SendMessageVideo { Agentid = cfg.AgentId, Touser = meta.Touser, Toparty = meta.Toparty, Totag = meta.Totag, Video = video, EnableDuplicateCheck = meta.EnableDuplicateCheck, Safe = meta.Safe, EnableIdTrans = meta.EnableIdTrans, DuplicateCheckInterval = meta.DuplicateCheckInterval };
            return PostMessageAsync(serviceName, model);
        }
        public Task<SendMessageResponse?> SendTextCardMessageAsync(string serviceName, MessageBase meta, MessageTextCard card)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return Task.FromResult<SendMessageResponse?>(null);
            var model = new SendMessageTextCard { Agentid = cfg.AgentId, Touser = meta.Touser, Toparty = meta.Toparty, Totag = meta.Totag, TextCard = card, EnableDuplicateCheck = meta.EnableDuplicateCheck, Safe = meta.Safe, EnableIdTrans = meta.EnableIdTrans, DuplicateCheckInterval = meta.DuplicateCheckInterval };
            return PostMessageAsync(serviceName, model);
        }
        public Task<SendMessageResponse?> SendNewsMessageAsync(string serviceName, MessageBase meta, MessageNews news)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return Task.FromResult<SendMessageResponse?>(null);
            var model = new SendMessageNews { Agentid = cfg.AgentId, Touser = meta.Touser, Toparty = meta.Toparty, Totag = meta.Totag, News = news, EnableDuplicateCheck = meta.EnableDuplicateCheck, Safe = meta.Safe, EnableIdTrans = meta.EnableIdTrans, DuplicateCheckInterval = meta.DuplicateCheckInterval };
            return PostMessageAsync(serviceName, model);
        }
        #endregion

        #region 回调
        public bool CheckSignature(string serviceName, Callback callback)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return false;
            var sign = GenerateSignature(serviceName, callback);
            return sign == callback.msg_signature;
        }
        public string GenerateSignature(string serviceName, Callback callback)
        {
            var cfg = GetCfg(serviceName) ?? throw new InvalidOperationException("config missing");
            var list = new List<string> { cfg.Token, callback.timestamp, callback.nonce, callback.echostr };
            list.Sort(StringComparer.Ordinal);
            var raw = string.Concat(list);
            using var sha = SHA1.Create();
            var hash = sha.ComputeHash(Encoding.ASCII.GetBytes(raw));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        public string DecryptEchostr(string serviceName, Callback callback)
        {
            if (!CheckSignature(serviceName, callback)) return string.Empty;
            var cfg = GetCfg(serviceName); if (cfg == null) return string.Empty;
            var data = WecomCryptography.AesDecrypt(callback.echostr ?? string.Empty, cfg.EncodingAesKey, out string corpId);
            if (corpId != cfg.CorpID) return string.Empty;
            return data;
        }
        public string DecryptCallBackData(string serviceName, string payload)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return string.Empty;
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(payload);
                var root = xml.FirstChild!;
                var encryptNode = root.SelectSingleNode("Encrypt");
                if (encryptNode == null)
                {
                    if (cfg.EnableLog)
                        _logger.LogWarning("[DecryptCallBackData:{Name}] 未找到 Encrypt 节点\n{Xml}", serviceName, payload);
                    return string.Empty;
                }
                var encrypt = encryptNode.InnerText.Trim();
                var data = WecomCryptography.AesDecrypt(encrypt, cfg.EncodingAesKey, out string corpId);
                if (corpId != cfg.CorpID) return string.Empty;
                return data;
            }
            catch (Exception ex)
            {
                if (cfg.EnableLog)
                {
                    _logger.LogError("[DecryptCallBackData]解密回调数据异常[{0}]:{1}\n{2}\n{3}", serviceName, ex.Message, ex.StackTrace, payload);
                }
                return string.Empty;
            }
        }
        public string EncryptCallBackData(string serviceName, string payload)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return string.Empty;
            try
            {
                var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                var nonce = Guid.NewGuid().ToString("n");
                var encryptReplay = WecomCryptography.AesEncrypt(payload, cfg.EncodingAesKey, cfg.CorpID);
                var cb = new Callback { echostr = encryptReplay, nonce = nonce, timestamp = timestamp };
                var replaySign = GenerateSignature(serviceName, cb);
                var sb = new StringBuilder(512);
                sb.AppendLine("<xml>")
                  .AppendLine($"\t<Encrypt><![CDATA[{encryptReplay}]]></Encrypt>")
                  .AppendLine($"\t<MsgSignature><![CDATA[{replaySign}]]></MsgSignature>")
                  .AppendLine($"\t<TimeStamp>{timestamp}</TimeStamp>")
                  .AppendLine($"\t<Nonce><![CDATA[{nonce}]]></Nonce>")
                  .AppendLine("</xml>");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[unified:{Name}] 加密回调失败", serviceName);
                return string.Empty;
            }
        }
        public string SendCallbackTextMessage(string serviceName, string userId, string content)
        {
            var cfg = GetCfg(serviceName); if (cfg == null) return string.Empty;
            var sb = new StringBuilder(256);
            sb.AppendLine("<xml>")
              .AppendLine($"\t<ToUserName><![CDATA[{userId}]]></ToUserName>")
              .AppendLine($"\t<FromUserName><![CDATA[{cfg.CorpID}]]></FromUserName>")
              .AppendLine($"\t<CreateTime>{DateTimeOffset.Now.ToUnixTimeSeconds()}</CreateTime>")
              .AppendLine("\t<MsgType><![CDATA[text]]></MsgType>")
              .AppendLine($"\t<Content><![CDATA[{content}]]></Content>")
              .AppendLine("</xml>");
            return EncryptCallBackData(serviceName, sb.ToString());
        }
        public MessageReceive? ResolvePayload(string payload)
        {
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(payload);
                var root = xml.FirstChild!;
                var createTime = root["CreateTime"]?.InnerText ?? "0";
                var agentId = root["AgentID"]?.InnerText ?? "0";
                var model = new MessageReceive
                {
                    Payload = payload,
                    CreateTime = int.Parse(createTime),
                    AgentId = int.Parse(agentId),
                    MsgType = root["MsgType"]?.InnerText ?? string.Empty,
                    EventName = root["Event"]?.InnerText ?? string.Empty,
                    ChangeType = root["ChangeType"]?.InnerText ?? string.Empty,
                    fromUserName = root["FromUserName"]?.InnerText ?? string.Empty,
                    ToUserName = root["ToUserName"]?.InnerText ?? string.Empty,
                    ExpiredTime = root["ExpiredTime"]?.InnerText ?? string.Empty,
                    Latitude = root["Latitude"]?.InnerText ?? string.Empty,
                    Longitude = root["Longitude"]?.InnerText ?? string.Empty,
                    Precision = root["Precision"]?.InnerText ?? string.Empty,
                    Content = root["Content"]?.InnerText ?? string.Empty,
                    MessageId = root["MsgId"]?.InnerText ?? string.Empty,
                    EventKey = root["EventKey"]?.InnerText ?? string.Empty,
                };
                if (model.EventName == "batch_job_result")
                {
                    model.BatchJob = new BatchJob
                    {
                        JobId = root["BatchJob"]?["JobId"]?.InnerText ?? string.Empty,
                        JobType = root["BatchJob"]?["JobType"]?.InnerText ?? string.Empty,
                        ErrCode = root["BatchJob"]?["ErrCode"]?.InnerText ?? string.Empty,
                        ErrMsg = root["BatchJob"]?["ErrMsg"]?.InnerText ?? string.Empty,
                    };
                }
                if (model.EventName == "change_contact" && model.ChangeType == "update_tag")
                {
                    var tagId = root["UpdateTag"]?["TagId"]?.InnerText ?? string.Empty;
                    model.UpdateTag = new UpdateTag
                    {
                        TagId = string.IsNullOrEmpty(tagId) ? 0 : int.Parse(tagId),
                        AddPartyItems = (root["UpdateTag"]?["AddPartyItems"]?.InnerText ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        DelPartyItems = (root["UpdateTag"]?["DelPartyItems"]?.InnerText ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        AddUserItems = (root["UpdateTag"]?["AddUserItems"]?.InnerText ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        DelUserItems = (root["UpdateTag"]?["DelUserItems"]?.InnerText ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    };
                }
                if (model.EventName == "change_contact" && model.ChangeType.EndsWith("te_user", StringComparison.Ordinal))
                {
                    model.UpdateUser = new UpdateUser
                    {
                        UserId = root["UserID"]?.InnerText ?? string.Empty,
                        NewUserId = root["NewUserId"]?.InnerText ?? string.Empty,
                        Department = (root["Department"]?.InnerText ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList(),
                    };
                }
                if (model.EventName == "scancode_push" || model.EventName == "scancode_waitmsg")
                {
                    model.ScanCodeInfo = new ScanCodeInfo
                    {
                        ScanType = root["ScanCodeInfo"]?["ScanType"]?.InnerText ?? string.Empty,
                        ScanResult = root["ScanCodeInfo"]?["ScanResult"]?.InnerText ?? string.Empty,
                    };
                }
                return model;
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}
