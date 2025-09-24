namespace DreamSlave.Wecom.Services
{
    /// <summary>
    /// 提供类似 IHttpClientFactory 的企业微信服务工厂，通过名称获取对应实例。
    /// </summary>
    public interface IWecomFactory
    {
        IWecomOAuth2Service GetOAuth2(string name);
        IWecomCallBackService GetCallback(string name);
        IWecomBotService GetBotService(string name);
        IWecomMessageService GetMessageService(string name);
    }
}
