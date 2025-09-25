
namespace DreamSlave.Wecom.Services
{
    public interface IWecomBotService
    {
        Task<bool> SendFileToBotAsync(string mediaId);
        Task<bool> SendNewsToBotAsync(List<Article> articles);
        Task<bool> SendImageToBotAsync(string fileBase64, string md5);
        Task<bool> SendMarkDownMessageToBotAsync(string text);
        Task<bool> SendMarkDownV2MessageToBotAsync(string text);
        Task<bool> SendTextMessageToBotAsync(string text);
        Task<string> UploadFileToBotAsync(byte[] fileBytes, string fileName);
    }
}