namespace DreamSlave.Wecom.Models.Request.Message
{
    public class MessageTextCard
    {
        /// <summary>
        /// 标题，不超过128个字符，超过会自动截断（支持id转译）
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// 描述，不超过512个字符，超过会自动截断（支持id转译）
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// 点击后跳转的链接。最长2048字节，请确保包含了协议头(http/https)
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// 按钮文字。 默认为“详情”， 不超过4个文字，超过自动截断。
        /// </summary>
        [JsonPropertyName("btntxt")]
        public string ButtonText { get; set; }
    }
}
