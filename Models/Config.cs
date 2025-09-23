namespace DreamSlave.Wecom.Models
{
    public class Config
    {
        /// <summary>
        /// 企业微信Id
        /// </summary>
        public string CorpID { get; set; }

        /// <summary>
        /// 企业SECRET
        /// </summary>
        public string CorpSecret { get; set; }

        /// <summary>
        /// 应用Id
        /// </summary>
        public int AgentId { get; set; }

        /// <summary>
        /// 消息回调Token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 消息回调aes key
        /// </summary>
        public string EncodingAesKey { get; set; } = string.Empty;

        /// <summary>
        /// 自动刷新token和ticket，默认为true，为false时需要手动调用刷新方法
        /// </summary>
        public bool AutoRefresh { get; set; } = true;
    }
}
