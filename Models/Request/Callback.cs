namespace DreamSlave.Wecom.Models.Request
{
    public class Callback
    {
        /// <summary>
        /// 企业微信加密签名，msg_signature结合了企业填写的token、请求中的timestamp、nonce参数、加密的消息体
        /// </summary>
        public string msg_signature { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public string timestamp { get; set; }

        /// <summary>
        /// 随机数
        /// </summary>
        public string nonce { get; set; }

        /// <summary>
        /// 加密的字符串。需要解密得到消息内容明文，解密后有random、msg_len、msg、CorpID四个字段，其中msg即为消息内容明文
        /// </summary>
        public string? echostr { get; set; } = "";
    }
}
