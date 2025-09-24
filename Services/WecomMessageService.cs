namespace DreamSlave.Wecom.Services
{
    /// <summary>
    /// 企业微信应用通知服务
    /// </summary>
    public class WecomMessageService : IWecomMessageService
    {
        private readonly ILogger<WecomMessageService> _logger;
        private readonly IWecomOAuth2Service _wecomOAuth2Service;
        private readonly IHttpClientFactory _httpClientFactory;

        public WecomMessageService(
            ILogger<WecomMessageService> logger,
            IHttpClientFactory httpClientFactory,
            IWecomOAuth2Service wecomOAuth2Service)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _wecomOAuth2Service = wecomOAuth2Service;
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private async Task<SendMessageResponse> SendMessageRequestAsync(object obj)
        {
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token=" + _wecomOAuth2Service.GetToken();
            using var client = _httpClientFactory.CreateClient("wecom_client");
            var body = JsonSerializer.Serialize(obj, new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var result = await client.PostAsync(url, content);
            var reposeText = await result.Content.ReadAsStringAsync();
            var respose = JsonSerializer.Deserialize<SendMessageResponse>(reposeText);
            return respose;
        }

        /// <summary>
        /// 发送文本消息给指定的用户
        /// </summary>
        public async Task<SendMessageResponse> SendTextMessageAsync(string content, params string[] userIds)
        {
            var config = _wecomOAuth2Service.GetConfig();
            var model = new SendMessageText()
            {
                Agentid = config.AgentId,
                Text = new MessageContent()
                {
                    Content = content,
                },
                Totag = "",
                Toparty = "",
                Touser = string.Join("|", userIds),
            };
            return await SendMessageRequestAsync(model);
        }

        /// <summary>
        /// 发送markdown消息给指定的用户
        /// </summary>
        public async Task<SendMessageResponse> SendMarkDownMessageAsync(string content, params string[] userIds)
        {
            var config = _wecomOAuth2Service.GetConfig();
            var model = new SendMessageMD()
            {
                Agentid = config.AgentId,
                Markdown = new MessageContent()
                {
                    Content = content,
                },
                Totag = "",
                Toparty = "",
                Touser = string.Join("|", userIds),
            };
            return await SendMessageRequestAsync(model);
        }

        /// <summary>
        /// 发送文本消息
        /// </summary>
        public async Task<SendMessageResponse> SendTextMessageAsync(MessageBase messageBase, MessageContent messageContent)
        {
            var config = _wecomOAuth2Service.GetConfig();
            var model = new SendMessageText()
            {
                Agentid = config.AgentId,
                DuplicateCheckInterval = messageBase.DuplicateCheckInterval,
                Touser = messageBase.Touser,
                Toparty = messageBase.Toparty,
                Totag = messageBase.Totag,
                EnableDuplicateCheck = messageBase.EnableDuplicateCheck,
                EnableIdTrans = messageBase.EnableIdTrans,
                Safe = messageBase.Safe,
                Text = messageContent,
            };
            return await SendMessageRequestAsync(model);
        }

        /// <summary>
        /// 发送markdown消息
        /// </summary>
        public async Task<SendMessageResponse> SendMarkDownMessageAsync(MessageBase messageBase, MessageContent messageContent)
        {
            var config = _wecomOAuth2Service.GetConfig();
            var model = new SendMessageMD()
            {
                Agentid = config.AgentId,
                DuplicateCheckInterval = messageBase.DuplicateCheckInterval,
                Touser = messageBase.Touser,
                Toparty = messageBase.Toparty,
                Totag = messageBase.Totag,
                EnableDuplicateCheck = messageBase.EnableDuplicateCheck,
                EnableIdTrans = messageBase.EnableIdTrans,
                Safe = messageBase.Safe,
                Markdown = messageContent,
            };
            return await SendMessageRequestAsync(model);
        }

        /// <summary>
        /// 发送图片消息
        /// </summary>
        public async Task<SendMessageResponse> SendImageMessageAsync(MessageBase messageBase, MessageFile messageFile)
        {
            var config = _wecomOAuth2Service.GetConfig();
            var model = new SendMessageImage()
            {
                Agentid = config.AgentId,
                DuplicateCheckInterval = messageBase.DuplicateCheckInterval,
                Touser = messageBase.Touser,
                Toparty = messageBase.Toparty,
                Totag = messageBase.Totag,
                EnableDuplicateCheck = messageBase.EnableDuplicateCheck,
                EnableIdTrans = messageBase.EnableIdTrans,
                Safe = messageBase.Safe,
                Image = messageFile,
            };
            return await SendMessageRequestAsync(model);
        }

        /// <summary>
        /// 发送语音消息
        /// </summary>
        public async Task<SendMessageResponse> SendVoiceMessageAsync(MessageBase messageBase, MessageFile messageFile)
        {
            var config = _wecomOAuth2Service.GetConfig();
            var model = new SendMessageVoice()
            {
                Agentid = config.AgentId,
                DuplicateCheckInterval = messageBase.DuplicateCheckInterval,
                Touser = messageBase.Touser,
                Toparty = messageBase.Toparty,
                Totag = messageBase.Totag,
                EnableDuplicateCheck = messageBase.EnableDuplicateCheck,
                EnableIdTrans = messageBase.EnableIdTrans,
                Safe = messageBase.Safe,
                Voice = messageFile,
            };
            return await SendMessageRequestAsync(model);
        }

        /// <summary>
        /// 发送视频消息
        /// </summary>
        public async Task<SendMessageResponse> SendVideoMessageAsync(MessageBase messageBase, MessageVideo messageVideo)
        {
            var config = _wecomOAuth2Service.GetConfig();
            var model = new SendMessageVideo()
            {
                Agentid = config.AgentId,
                DuplicateCheckInterval = messageBase.DuplicateCheckInterval,
                Touser = messageBase.Touser,
                Toparty = messageBase.Toparty,
                Totag = messageBase.Totag,
                EnableDuplicateCheck = messageBase.EnableDuplicateCheck,
                EnableIdTrans = messageBase.EnableIdTrans,
                Safe = messageBase.Safe,
                Video = messageVideo,
            };
            return await SendMessageRequestAsync(model);
        }

        /// <summary>
        /// 发送文本卡片消息
        /// </summary>
        public async Task<SendMessageResponse> SendTextCardMessageAsync(MessageBase messageBase, MessageTextCard messageTextCard)
        {
            var config = _wecomOAuth2Service.GetConfig();
            var model = new SendMessageTextCard()
            {
                Agentid = config.AgentId,
                DuplicateCheckInterval = messageBase.DuplicateCheckInterval,
                Touser = messageBase.Touser,
                Toparty = messageBase.Toparty,
                Totag = messageBase.Totag,
                EnableDuplicateCheck = messageBase.EnableDuplicateCheck,
                EnableIdTrans = messageBase.EnableIdTrans,
                Safe = messageBase.Safe,
                TextCard = messageTextCard,
            };
            return await SendMessageRequestAsync(model);
        }

        /// <summary>
        /// 发送图文消息
        /// </summary>
        public async Task<SendMessageResponse> SendNewsMessageAsync(MessageBase messageBase, MessageNews messageNews)
        {
            var config = _wecomOAuth2Service.GetConfig();
            var model = new SendMessageNews()
            {
                Agentid = config.AgentId,
                DuplicateCheckInterval = messageBase.DuplicateCheckInterval,
                Touser = messageBase.Touser,
                Toparty = messageBase.Toparty,
                Totag = messageBase.Totag,
                EnableDuplicateCheck = messageBase.EnableDuplicateCheck,
                EnableIdTrans = messageBase.EnableIdTrans,
                Safe = messageBase.Safe,
                News = messageNews,
            };
            return await SendMessageRequestAsync(model);
        }
    }
}
