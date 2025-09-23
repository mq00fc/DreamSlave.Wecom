namespace DreamSlave.Wecom.Models.Response.UserInfo
{
    /// <summary>
    /// OAuth2用户身份
    /// </summary>
    /// <remarks>
    /// https://developer.work.weixin.qq.com/document/path/91023
    /// </remarks>
    public class OAuth2UserInfo
    {
        /// <summary>
        /// 返回码
        /// </summary>
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        /// <summary>
        /// 对返回码的文本描述内容
        /// </summary>
        [JsonPropertyName("errmsg")]
        public string Errmsg { get; set; }

        /// <summary>
        /// 成员UserID。若需要获得用户详情信息，可调用通讯录接口：读取成员。如果是互联企业/企业互联/上下游，则返回的UserId格式如：CorpId/userid
        /// </summary>
        [JsonPropertyName("userid")]
        public string Userid { get; set; }

        /// <summary>
        /// 成员票据，最大为512字节，有效期为1800s。scope为snsapi_privateinfo，且用户在应用可见范围之内时返回此参数。后续利用该参数可以获取用户信息或敏感信息，参见"获取访问用户敏感信息"。暂时不支持上下游或/企业互联场景
        /// </summary>
        [JsonPropertyName("user_ticket")]
        public string UserTicket { get; set; }

        /// <summary>
        /// 非企业成员的标识，对当前企业唯一。不超过64字节
        /// </summary>
        [JsonPropertyName("openid")]
        public string Openid { get; set; }

        /// <summary>
        /// 外部联系人id，当且仅当用户是企业的客户，且跟进人在应用的可见范围内时返回。如果是第三方应用调用，针对同一个客户，同一个服务商不同应用获取到的id相同
        /// </summary>
        [JsonPropertyName("external_userid")]
        public string ExternalUserid { get; set; }
    }
}
