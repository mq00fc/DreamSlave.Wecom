namespace DreamSlave.Wecom.Services
{
    public interface IWecomCallBackService
    {
        bool CheckSignature(Callback callback);
        bool CheckSignature(string token, Callback callback);
        string DecryptCallBackData(string payload);
        string DecryptCallBackData(string payload, string aesKey, string corpId);
        string DecryptEchostr(Callback callback);
        string DecryptEchostr(string aesKey, Callback callback);
        string EncryptCallBackData(string payload);
        string EncryptCallBackData(string payload, string aesKey, string corpId);
        string GenerateSignature(Callback callback);
        string GenerateSignature(string token, Callback callback);
        string SendTextMessage(string userId, string content);
        string SendTextMessage(string userId, string content, string corpId);
    }
}