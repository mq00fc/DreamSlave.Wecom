using System.Web;

namespace DreamSlave.Wecom.Services
{
    public class WecomOAuth2Service : IWecomOAuth2Service
    {
        private readonly ILogger<WecomOAuth2Service> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<Models.Config> _options;
        private readonly IMemoryCache _memoryCache;
        public WecomOAuth2Service(
            ILogger<WecomOAuth2Service> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<Models.Config> options,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _options = options;
            _memoryCache = memoryCache;
        }

        private string CacheKey(string name) => $"wecom:{_options.Value.CorpID}:{_options.Value.AgentId}:{name}";

        /// <summary>
        /// 获取token
        /// </summary>
        /// <returns></returns>
        public string GetToken()
        {
            return _memoryCache.Get<string>(CacheKey("token"));
        }

        /// <summary>
        /// 获取jsapi-ticket
        /// </summary>
        /// <returns></returns>
        public string GetTicket()
        {
            return _memoryCache.Get<string>(CacheKey("ticket"));
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <returns></returns>
        public Models.Config GetConfig()
        {
            return _options.Value;
        }


        /// <summary>
        /// 获取jsapi-dto
        /// </summary>
        /// <param name="url">需要使用的页面url</param>
        /// <returns></returns>
        public JsapiTicketDto GetJsapiTicketDto(string url)
        {
            if (_options.Value.AgentId < 1)
            {
                _logger.LogError("获取JsapiTicketDto失败:AgentId未配置");
                return null;
            }

            var ticket = GetTicket();
            if (string.IsNullOrEmpty(ticket))
            {
                _logger.LogError("获取JsapiTicketDto失败:JsapiTicket为空");
                return null;
            }

            // 规范：url 不包含 # 之后的部分
            var sharpIndex = url.IndexOf('#');
            if (sharpIndex >= 0)
            {
                url = url[..sharpIndex];
            }

            var dto = new JsapiTicketDto
            {
                CorpId = _options.Value.CorpID,
                AgentId = _options.Value.AgentId,
                Ticket = ticket,
                Nonce = Guid.NewGuid().ToString("N"),
                TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            };

            // 使用有序字典保证参数按字典序排列
            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "jsapi_ticket", dto.Ticket },
                { "noncestr", dto.Nonce },
                { "timestamp", dto.TimeStamp },
                { "url", url }
            };

            // 按顺序拼接，避免额外的 LINQ 分配，严格控制顺序
            StringBuilder sb = new StringBuilder(1024);
            foreach (var item in parameters)
            {
                sb.Append($"{item.Key}={item.Value}&");
            }
            var signSource = WebUtility.UrlDecode(sb.ToString().Trim('&'));

            byte[] hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(signSource));
            var sigBuilder = new StringBuilder(hashBytes.Length * 2);

            foreach (var b in hashBytes)
                sigBuilder.Append(b.ToString("x2"));

            dto.Signature = sigBuilder.ToString();
            return dto;
        }

        /// <summary>
        /// 刷新获取AccessToken
        /// </summary>
        /// <returns></returns>
        public async Task<WecomAccessToken> RefreshAccessTokenAsync()
        {
            using var client = _httpClientFactory.CreateClient("wecom_client");
            using var response = await client.GetAsync($"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={_options.Value.CorpID}&corpsecret={_options.Value.CorpSecret}");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("获取AccessToken失败:statuscode:{0}\ncontent:{1}", response.StatusCode, content);
                return null;
            }
            var model = JsonSerializer.Deserialize<WecomAccessToken>(content);
            if (model?.Errcode != 0 && _options.Value.EnableLog)
            {
                _logger.LogError("[wecom]获取access_token失败:{0}", content);
            }
            //如果不为null则增加到内存缓存中
            if (!string.IsNullOrEmpty(model?.AccessToken))
            {
                _memoryCache.Set(CacheKey("token"), model.AccessToken, TimeSpan.FromSeconds(model.ExpiresIn));
            }
            return model;
        }


        /// <summary>
        /// 刷新获取jsapi-ticket
        /// </summary>
        /// <returns></returns>
        public async Task<WecomJsapiTicket> RefreshJsApiTicketAsync()
        {
            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("获取jsapi-ticket失败:AccessToken为空");
                return null;
            }

            using var client = _httpClientFactory.CreateClient("wecom_client");
            using var response = await client.GetAsync($"https://qyapi.weixin.qq.com/cgi-bin/get_jsapi_ticket?access_token={token}");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("获取jsapi-ticket失败:statuscode:{0}\ncontent:{1}", response.StatusCode, content);
                return null;
            }
            var model = JsonSerializer.Deserialize<WecomJsapiTicket>(content);
            if(model?.Errcode != 0 && _options.Value.EnableLog)
            {
                _logger.LogError("[wecom]获取jsapi-ticket失败:{0}", content);
            }
            //如果不为null则增加到内存缓存中
            if (!string.IsNullOrEmpty(model?.Ticket))
            {
                _memoryCache.Set(CacheKey("ticket"), model.Ticket, TimeSpan.FromSeconds(model.ExpiresIn));
            }
            return model;
        }

        #region 网页授权登录

        /// <summary>
        /// 构建OAuth2授权链接
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public string BuildOAuth2Url(string url, string state)
        {
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("https://open.weixin.qq.com/connect/oauth2/authorize?");
            sb.Append($"appid={_options.Value.CorpID}&");
            sb.Append("redirect_uri=");
            sb.Append(WebUtility.UrlEncode(url));
            sb.Append("&response_type=code&");
            sb.Append("scope=snsapi_privateinfo&");
            sb.Append($"state={state}&");
            sb.Append($"agentid={_options.Value.AgentId}");
            sb.Append("#wechat_redirect");

            return sb.ToString();
        }


        /// <summary>
        /// 获取授权用户登录身份
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<OAuth2UserInfo> GetOAuth2UserInfoAsync(string code)
        {
            var token = GetToken();
            using var client = _httpClientFactory.CreateClient("wecom_client");
            using var response = await client.GetAsync($"https://qyapi.weixin.qq.com/cgi-bin/auth/getuserinfo?access_token={token}&code={code}");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("获取授权用户登录身份失败:statuscode:{0}\ncontent:{1}", response.StatusCode, content);
                return null;
            }
            var model = JsonSerializer.Deserialize<OAuth2UserInfo>(content);
            if (model?.Errcode != 0 && _options.Value.EnableLog)
            {
                _logger.LogError("[wecom]获取授权用户登陆身份失败:{0}", content);
            }
            return model;
        }

        /// <summary>
        /// 获取用户敏感信息
        /// </summary>
        /// <param name="oAuth2UserInfo"></param>
        /// <returns></returns>
        public async Task<OAuth2UserDetail> GetOAuth2UserDetailAsync(OAuth2UserInfo oAuth2UserInfo)
        {
            var token = GetToken();
            using var client = _httpClientFactory.CreateClient("wecom_client");
            using var response = await client.PostAsync($"https://qyapi.weixin.qq.com/cgi-bin/auth/getuserdetail?access_token={token}",
                new StringContent($"{{\"user_ticket\": \"{oAuth2UserInfo.UserTicket}\"}}", Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("获取用户敏感信息失败:statuscode:{0}\ncontent:{1}", response.StatusCode, content);
                return null;
            }
            var model = JsonSerializer.Deserialize<OAuth2UserDetail>(content);
            if (model?.Errcode != 0 && _options.Value.EnableLog)
            {
                _logger.LogError("[wecom]获取用户敏感信息失败失败:{0}", content);
            }
            return model;
        }

        #endregion


        #region 企业微信Web登录
        /// <summary>
        /// 构建Web登录
        /// </summary>
        /// <param name="url"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public string BuildWebLoginUrl(string url, string state)
        {
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("https://login.work.weixin.qq.com/wwlogin/sso/login?");
            sb.Append("login_type=CorpApp&");
            sb.Append($"appid={_options.Value.CorpID}&");
            sb.Append($"agentid={_options.Value.AgentId}&");
            sb.Append("redirect_uri=");
            sb.Append(WebUtility.UrlEncode(url));
            sb.Append($"&state={state}&");
            sb.Append("#wechat_redirect");

            return sb.ToString();
        }

        /// <summary>
        /// 构建web登录前端需要参数
        /// </summary>
        /// <param name="url"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public WebLoginDto BuildWebLoginDto(string url, string state)
        {
            return new WebLoginDto()
            {
                AgentId = _options.Value.AgentId,
                AppId = _options.Value.CorpID,
                LoginType = "CorpApp",
                Lang = "zh",
                RedirectUri = url,
                State = state
            };
        }

        /// <summary>
        /// 获取企业微信web登录的用户信息
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<WebLoginUserInfo> GetWebLoginUserInfoAsync(string code)
        {
            var token = GetToken();
            using var client = _httpClientFactory.CreateClient("wecom_client");
            using var response = await client.GetAsync($"https://qyapi.weixin.qq.com/cgi-bin/auth/getuserinfo?access_token={token}&code={code}");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("获取Web登录用户身份失败:statuscode:{0}\ncontent:{1}", response.StatusCode, content);
                return null;
            }
            var model = JsonSerializer.Deserialize<WebLoginUserInfo>(content);
            if (model?.Errcode != 0 && _options.Value.EnableLog)
            {
                _logger.LogError("[wecom]获取web登录用户身份失败:{0}", content);
            }
            return model;
        }
        #endregion
    }
}
