namespace DreamSlave.Wecom.Models.Request.Bot
{
    public class Article
    {
        /// <summary>
        /// 标题，不超过128个字节，超过会自动截断
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// 描述，不超过512个字节，超过会自动截断
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// 点击后跳转的链接
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// 图文消息的图片链接，支持JPG、PNG格式，较好的效果为大图 1068*455，小图150*150。
        /// </summary>
        [JsonPropertyName("picurl")]
        public string PicUrl { get; set; }
    }
}
