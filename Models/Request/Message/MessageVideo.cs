namespace DreamSlave.Wecom.Models.Request.Message
{
    public class MessageVideo : MessageFile
    {
        /// <summary>
        /// 视频消息的标题，不超过128个字节，超过会自动截断
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }


        /// <summary>
        /// 视频消息的描述，不超过512个字节，超过会自动截断
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
