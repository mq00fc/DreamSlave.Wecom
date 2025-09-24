namespace DreamSlave.Wecom.Models.Request.Message
{
    public class SendMessageText : MessageBase
    {
        [JsonPropertyName("msgtype")]
        public string Msgtype { get; set; } = "text";

        /// <summary>
        /// 企业应用的id，整型。企业内部开发，可在应用的设置页面查看；第三方服务商，可通过接口 获取企业授权信息 获取该参数值
        /// </summary>
        [JsonPropertyName("agentid")]
        public long Agentid { get; set; }

        /// <summary>
        /// 消息内容，最长不超过2048个字节，超过将截断（支持id转译）
        /// </summary>
        [JsonPropertyName("text")]
        public MessageContent Text { get; set; }
    }
}
