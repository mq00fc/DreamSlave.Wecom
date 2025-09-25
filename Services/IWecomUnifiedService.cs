namespace DreamSlave.Wecom.Services
{
    /// <summary>
    /// ͳһ��ʵ�� WeCom ����ӿڣ����� / OAuth / ��Ϣ���� / �ص��ӽ��� ȫ����װ��
    /// </summary>
    public interface IWecomUnifiedService
    {
        // ����/����/Ʊ��
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

        // ��Ϣ���ͣ��� & �߼���
        Task<SendMessageResponse?> SendTextMessageAsync(string serviceName, string content, params string[] userIds);
        Task<SendMessageResponse?> SendMarkDownMessageAsync(string serviceName, string content, params string[] userIds);
        Task<SendMessageResponse?> SendTextMessageAsync(string serviceName, MessageBase meta, MessageContent content);
        Task<SendMessageResponse?> SendMarkDownMessageAsync(string serviceName, MessageBase meta, MessageContent content);
        Task<SendMessageResponse?> SendImageMessageAsync(string serviceName, MessageBase meta, MessageFile file);
        Task<SendMessageResponse?> SendVoiceMessageAsync(string serviceName, MessageBase meta, MessageFile file);
        Task<SendMessageResponse?> SendVideoMessageAsync(string serviceName, MessageBase meta, MessageVideo video);
        Task<SendMessageResponse?> SendTextCardMessageAsync(string serviceName, MessageBase meta, MessageTextCard card);
        Task<SendMessageResponse?> SendNewsMessageAsync(string serviceName, MessageBase meta, MessageNews news);

        // �ص���ǩ�� / �ӽ��� / ����
        bool CheckSignature(string serviceName, Callback callback);
        string GenerateSignature(string serviceName, Callback callback);
        string DecryptEchostr(string serviceName, Callback callback);
        string DecryptCallBackData(string serviceName, string payload);
        string EncryptCallBackData(string serviceName, string payload);
        string SendCallbackTextMessage(string serviceName, string userId, string content);
        MessageReceive? ResolvePayload(string payload);
    }
}
