namespace DreamSlave.Wecom.Models.Request.Message
{
    public class MessageBase
    {
        /// <summary>
        /// 指定接收消息的成员，成员ID列表（多个接收者用‘|’分隔，最多支持1000个）。特殊情况：指定为"@all"，则向该企业应用的全部成员发送
        /// </summary>
        [JsonPropertyName("touser")]
        public string Touser { get; set; } = null;

        /// <summary>
        /// 指定接收消息的部门，部门ID列表，多个接收者用‘|’分隔，最多支持100个。当touser为"@all"时忽略本参数
        /// </summary>
        [JsonPropertyName("toparty")]
        public string Toparty { get; set; } = null;

        /// <summary>
        /// 指定接收消息的标签，标签ID列表，多个接收者用‘|’分隔，最多支持100个。当touser为"@all"时忽略本参数
        /// </summary>
        [JsonPropertyName("totag")]
        public string Totag { get; set; } = null;

        /// <summary>
        /// 表示是否是保密消息，0表示可对外分享，1表示不能分享且内容显示水印，默认为0
        /// </summary>
        [JsonPropertyName("safe")]
        public long Safe { get; set; } = 0;

        /// <summary>
        /// 表示是否开启id转译，0表示否，1表示是，默认0。
        /// </summary>
        [JsonPropertyName("enable_id_trans")]
        public long EnableIdTrans { get; set; } = 0;

        /// <summary>
        /// 表示是否开启重复消息检查，0表示否，1表示是，默认0
        /// </summary>
        [JsonPropertyName("enable_duplicate_check")]
        public long EnableDuplicateCheck { get; set; } = 0;

        /// <summary>
        /// 表示是否重复消息检查的时间间隔，默认1800s，最大不超过4小时
        /// </summary>
        [JsonPropertyName("duplicate_check_interval")]
        public long DuplicateCheckInterval { get; set; } = 0;
    }
}
