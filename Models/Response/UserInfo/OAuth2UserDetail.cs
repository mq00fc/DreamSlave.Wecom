namespace DreamSlave.Wecom.Models.Response.UserInfo
{
    /// <summary>
    /// 获取用户敏感信息
    /// </summary>
    /// <remarks>
    /// https://developer.work.weixin.qq.com/document/path/95833
    /// </remarks>
    public class OAuth2UserDetail
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
        /// 成员UserID
        /// </summary>
        [JsonPropertyName("userid")]
        public string Userid { get; set; }

        /// <summary>
        /// 性别。0表示未定义，1表示男性，2表示女性。仅在用户同意snsapi_privateinfo授权时返回真实值，否则返回0.
        /// </summary>
        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        /// <summary>
        /// 头像url。仅在用户同意snsapi_privateinfo授权时返回真实头像，否则返回默认头像
        /// </summary>
        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }

        /// <summary>
        /// 员工个人二维码（扫描可添加为外部联系人），仅在用户同意snsapi_privateinfo授权时返回
        /// </summary>
        [JsonPropertyName("qr_code")]
        public string QrCode { get; set; }

        /// <summary>
        /// 手机，仅在用户同意snsapi_privateinfo授权时返回，第三方应用不可获取
        /// </summary>
        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }

        /// <summary>
        /// 邮箱，仅在用户同意snsapi_privateinfo授权时返回，第三方应用不可获取
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; }

        /// <summary>
        /// 企业邮箱，仅在用户同意snsapi_privateinfo授权时返回，第三方应用不可获取
        /// </summary>
        [JsonPropertyName("biz_mail")]
        public string BizMail { get; set; }

        /// <summary>
        /// 仅在用户同意snsapi_privateinfo授权时返回，第三方应用不可获取
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; }
    }
}
