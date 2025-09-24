namespace DreamSlave.Wecom.Models.Request.Message
{
    public class SendMessageMD : MessageBase
    {
        /// <summary>
        /// 消息类型，此时固定为：markdown
        /// </summary>
        [JsonPropertyName("msgtype")]
        public string Msgtype { get; set; } = "markdown";

        /// <summary>
        /// 企业应用的id，整型。企业内部开发，可在应用的设置页面查看；第三方服务商，可通过接口 获取企业授权信息 获取该参数值
        /// </summary>
        [JsonPropertyName("agentid")]
        public long Agentid { get; set; }

        /// <summary>
        /// markdown内容，最长不超过2048个字节，必须是utf8编码
        /// </summary>
        [JsonPropertyName("markdown")]
        public MessageContent Markdown { get; set; }
    }
}
