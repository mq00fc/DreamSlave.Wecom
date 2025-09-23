namespace DreamSlave.Wecom.Models.Response
{
    /// <summary>
    /// token model
    /// </summary>
    /// <remarks>
    /// https://developer.work.weixin.qq.com/document/path/91039
    /// </remarks>
    public class WecomAccessToken
    {
        /// <summary>
        /// 出错返回码，为0表示成功，非0表示调用失败
        /// </summary>
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        /// <summary>
        /// 返回码提示语
        /// </summary>
        [JsonPropertyName("errmsg")]
        public string Errmsg { get; set; }

        /// <summary>
        /// 获取到的凭证，最长为512字节
        /// </summary>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// 凭证的有效时间（秒）
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
