namespace DreamSlave.Wecom.Models.WebDto
{
    /// <summary>
    /// 企业微信jsapi_ticket返回模型
    /// </summary>
    public class JsapiTicketDto
    {
        /// <summary>
        /// 企业Id
        /// </summary>
        [JsonPropertyName("corpId")]
        public string CorpId { get; set; }

        /// <summary>
        /// 应用的AgentID
        /// </summary>
        [JsonPropertyName("agentId")]
        public int AgentId { get; set; }

        /// <summary>
        /// 随机字符串
        /// </summary>
        [JsonPropertyName("nonce")]
        public string Nonce { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string TimeStamp { get; set; }

        /// <summary>
        /// jsapi_ticket
        /// </summary>
        [JsonPropertyName("ticket")]
        public string Ticket { get; set; }

        /// <summary>
        /// 签名
        /// </summary>
        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }
}
