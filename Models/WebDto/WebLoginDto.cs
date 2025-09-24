namespace DreamSlave.Wecom.Models.WebDto
{
    /// <summary>
    /// weblogin前端需要参数
    /// </summary>
    /// <remarks>
    /// https://developer.work.weixin.qq.com/document/path/98152
    /// </remarks>
    public class WebLoginDto
    {
        /// <summary>
        /// 登录类型
        /// </summary>
        [JsonPropertyName("login_type")]
        public string LoginType { get; set; } = "CorpApp";

        /// <summary>
        /// 登录类型为企业自建应用/服务商代开发应用时填企业 CorpID
        /// </summary>
        [JsonPropertyName("appid")]
        public string AppId { get; set; }

        /// <summary>
        /// 企业自建应用/服务商代开发应用 AgentID，当login_type=CorpApp时填写
        /// </summary>
        [JsonPropertyName("agentid")]
        public int AgentId { get; set; }

        /// <summary>
        /// 登录成功重定向 url，需进行 URLEncode
        /// </summary>
        [JsonPropertyName("redirect_uri")]
        public string RedirectUri { get; set; }

        /// <summary>
        /// 用于保持请求和回调的状态，授权请求后原样带回给企业。该参数可用于防止CSRF 攻击（跨站请求伪造攻击），建议带上该参数
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; }

        /// <summary>
        /// 语言类型。zh：中文；en：英文。
        /// </summary>
        [JsonPropertyName("lang")]
        public string Lang { get; set; }
    }
}
