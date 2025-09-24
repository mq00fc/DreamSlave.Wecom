
namespace DreamSlave.Wecom.Services
{
    public interface IWecomMessageService
    {
        Task<SendMessageResponse> SendImageMessageAsync(MessageBase messageBase, MessageFile messageFile);
        Task<SendMessageResponse> SendMarkDownMessageAsync(MessageBase messageBase, MessageContent messageContent);
        Task<SendMessageResponse> SendMarkDownMessageAsync(string content, params string[] userIds);
        Task<SendMessageResponse> SendNewsMessageAsync(MessageBase messageBase, MessageNews messageNews);
        Task<SendMessageResponse> SendTextCardMessageAsync(MessageBase messageBase, MessageTextCard messageTextCard);
        Task<SendMessageResponse> SendTextMessageAsync(MessageBase messageBase, MessageContent messageContent);
        Task<SendMessageResponse> SendTextMessageAsync(string content, params string[] userIds);
        Task<SendMessageResponse> SendVideoMessageAsync(MessageBase messageBase, MessageVideo messageVideo);
        Task<SendMessageResponse> SendVoiceMessageAsync(MessageBase messageBase, MessageFile messageFile);
    }
}