namespace DreamSlave.Wecom.Models.CallBack
{
    public class MessageReceive
    {
        /// <summary>
        /// 消息负载
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        public int CreateTime { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public string MsgType { get; set; }

        /// <summary>
        /// 应用Id
        /// </summary>
        public int AgentId { get; set; }

        /// <summary>
        /// 事件类型
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 事件KEY值
        /// </summary>
        public string EventKey { get; set; }

        /// <summary>
        /// 改变类型
        /// </summary>
        public string ChangeType { get; set; }

        /// <summary>
        /// 企业用户Id
        /// </summary>
        public string fromUserName { get; set; }

        /// <summary>
        /// 企业微信Id
        /// </summary>
        public string ToUserName { get; set; }

        #region 地区
        /// <summary>
        /// 纬度
        /// </summary>
        public string Latitude { get; set; }

        /// <summary>
        /// 经度
        /// </summary>
        public string Longitude { get; set; }

        /// <summary>
        /// 精度?
        /// </summary>
        public string Precision { get; set; }
        #endregion

        #region 文本
        /// <summary>
        /// 过期时间
        /// </summary>
        public string ExpiredTime { get; set; }

        /// <summary>
        /// 文本内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 消息Id
        /// </summary>
        public string MessageId { get; set; }
        #endregion

        /// <summary>
        /// 异步任务完成回调
        /// </summary>
        public BatchJob BatchJob { get; set; } = null;

        /// <summary>
        /// 标签更新
        /// </summary>
        public UpdateTag UpdateTag { get; set; } = null;

        /// <summary>
        /// 用户信息修改/新增/删除
        /// </summary>
        public UpdateUser UpdateUser { get; set; } = null;

        /// <summary>
        /// 扫码信息
        /// </summary>
        public ScanCodeInfo ScanCodeInfo { get; set; } = null;
    }


    /// <summary>
    /// 异步任务完成通知
    /// </summary>
    /// <remarks>
    /// https://developer.work.weixin.qq.com/document/path/90973
    /// </remarks>
    public class BatchJob
    {
        public string JobId { get; set; }
        public string JobType { get; set; }
        public string ErrCode { get; set; }
        public string ErrMsg { get; set; }
    }


    /// <summary>
    /// 标签更新
    /// </summary>
    /// <remarks>
    /// https://developer.work.weixin.qq.com/document/path/90972
    /// </remarks>
    public class UpdateTag
    {
        public int TagId { get; set; }

        public List<string> AddUserItems { get; set; }
        public List<string> DelUserItems { get; set; }
        public List<string> AddPartyItems { get; set; }
        public List<string> DelPartyItems { get; set; }
    }

    /// <summary>
    /// 用户更新
    /// </summary>
    /// <remarks>
    /// https://developer.work.weixin.qq.com/document/path/90970
    /// </remarks>
    public class UpdateUser
    {
        /// <summary>
        /// 用户id
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 新的用户id
        /// </summary>
        public string NewUserId { get; set; }

        /// <summary>
        /// 部门id
        /// </summary>
        public List<int> Department { get; set; }
    }

    /// <summary>
    /// 扫码事件
    /// </summary>
    /// <remarks>
    /// https://developer.work.weixin.qq.com/document/path/90240
    /// </remarks>
    public class ScanCodeInfo
    {
        /// <summary>
        /// 扫码类型，一般是qrcode
        /// </summary>
        public string ScanType { get; set; }

        /// <summary>
        /// 扫描结果，即二维码对应的字符串信息
        /// </summary>
        public string ScanResult { get; set; }
    }
}
