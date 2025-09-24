namespace DreamSlave.Wecom.Services
{
    /// <summary>
    /// �ṩ���� IHttpClientFactory ����ҵ΢�ŷ��񹤳���ͨ�����ƻ�ȡ��Ӧʵ����
    /// </summary>
    public interface IWecomFactory
    {
        IWecomOAuth2Service GetOAuth2(string name);
        IWecomCallBackService GetCallback(string name);
        IWecomBotService GetBotService(string name);
        IWecomMessageService GetMessageService(string name);
    }
}
