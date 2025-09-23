
namespace DreamSlave.Wecom.Services
{
    public interface IWecomOAuth2Service
    {
        string BuildOAuth2Url(string url, string state);
        string BuildWebLoginUrl(string url, string state);
        JsapiTicketDto GetJsapiTicketDto(string url);
        Task<OAuth2UserDetail> GetOAuth2UserDetailAsync(OAuth2UserInfo oAuth2UserInfo);
        Task<OAuth2UserInfo> GetOAuth2UserInfoAsync(string code);
        string GetTicket();
        string GetToken();
        Task<WebLoginUserInfo> GetWebLoginUserInfoAsync(string code);
        Task<WecomAccessToken> RefreshAccessTokenAsync();
        Task<WecomJsapiTicket> RefreshJsApiTicketAsync();
    }
}