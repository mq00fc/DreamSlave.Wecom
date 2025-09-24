namespace DreamSlave.Wecom.Models.Request.Message
{
    public class SendMessageFile : MessageBase
    {
        /// <summary>
        /// 消息类型，此时固定为：image
        /// </summary>
        [JsonPropertyName("msgtype")]
        public string Msgtype { get; set; } = "file";

        /// <summary>
        /// 企业应用的id，整型。企业内部开发，可在应用的设置页面查看；第三方服务商，可通过接口 获取企业授权信息 获取该参数值
        /// </summary>
        [JsonPropertyName("agentid")]
        public long Agentid { get; set; }

        [JsonPropertyName("file")]
        public MessageFile File { get; set; }
    }
}
