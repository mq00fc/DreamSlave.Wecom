namespace DreamSlave.Wecom.Models.Request.Message
{
    public class MessageArticles
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

        /// <summary>
        /// 小程序appid，必须是与当前应用关联的小程序，appid和pagepath必须同时填写，填写后会忽略url字段
        /// </summary>
        [JsonPropertyName("appid")]
        public string AppId { get; set; }
        /// <summary>

        /// 点击消息卡片后的小程序页面，最长128字节，仅限本小程序内的页面。appid和pagepath必须同时填写，填写后会忽略url字段
        /// </summary>
        [JsonPropertyName("pagepath")]
        public string PagePath { get; set; }
    }
}
