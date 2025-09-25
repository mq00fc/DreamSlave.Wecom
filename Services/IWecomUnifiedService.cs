namespace DreamSlave.Wecom.Services
{
    /// <summary>
    /// 统一多实例 WeCom 服务接口（令牌 / OAuth / 消息发送 / 回调加解密 全部封装）
    /// </summary>
    public interface IWecomUnifiedService
    {
        // 配置/令牌/票据
        Models.Config? GetConfig(string serviceName = "default");
        string? GetToken(string serviceName = "default");
        string? GetTicket(string serviceName = "default");
        Task<WecomAccessToken?> RefreshAccessTokenAsync(string serviceName = "default");
        Task<WecomJsapiTicket?> RefreshJsApiTicketAsync(string serviceName = "default");
        JsapiTicketDto? GetJsapiTicketDto(string serviceName, string url);

        // OAuth / WebLogin
        string BuildOAuth2Url(string serviceName, string url, string state);
        string BuildWebLoginUrl(string serviceName, string url, string state);
        WebLoginDto BuildWebLoginDto(string serviceName, string url, string state);
        Task<OAuth2UserInfo?> GetOAuth2UserInfoAsync(string serviceName, string code);
        Task<OAuth2UserDetail?> GetOAuth2UserDetailAsync(string serviceName, OAuth2UserInfo info);
        Task<WebLoginUserInfo?> GetWebLoginUserInfoAsync(string serviceName, string code);

        // 消息发送（简化 & 高级）
        Task<SendMessageResponse?> SendTextMessageAsync(string serviceName, string content, params string[] userIds);
        Task<SendMessageResponse?> SendMarkDownMessageAsync(string serviceName, string content, params string[] userIds);
        Task<SendMessageResponse?> SendTextMessageAsync(string serviceName, MessageBase meta, MessageContent content);
        Task<SendMessageResponse?> SendMarkDownMessageAsync(string serviceName, MessageBase meta, MessageContent content);
        Task<SendMessageResponse?> SendImageMessageAsync(string serviceName, MessageBase meta, MessageFile file);
        Task<SendMessageResponse?> SendVoiceMessageAsync(string serviceName, MessageBase meta, MessageFile file);
        Task<SendMessageResponse?> SendVideoMessageAsync(string serviceName, MessageBase meta, MessageVideo video);
        Task<SendMessageResponse?> SendTextCardMessageAsync(string serviceName, MessageBase meta, MessageTextCard card);
        Task<SendMessageResponse?> SendNewsMessageAsync(string serviceName, MessageBase meta, MessageNews news);

        // 回调：签名 / 加解密 / 解析
        bool CheckSignature(string serviceName, Callback callback);
        string GenerateSignature(string serviceName, Callback callback);
        string DecryptEchostr(string serviceName, Callback callback);
        string DecryptCallBackData(string serviceName, string payload);
        string EncryptCallBackData(string serviceName, string payload);
        string SendCallbackTextMessage(string serviceName, string userId, string content);
        MessageReceive? ResolvePayload(string payload);
    }
}
