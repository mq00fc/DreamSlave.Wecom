namespace DreamSlave.Wecom.Models.Request.Message
{
    public class MessageFile
    {
        /// <summary>
        /// 图片媒体文件id，可以调用上传临时素材接口获取
        /// </summary>
        [JsonPropertyName("media_id")]
        public string MediaId { get; set; }
    }
}
