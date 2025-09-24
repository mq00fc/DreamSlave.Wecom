namespace DreamSlave.Wecom.Models.Response.Message
{
    public class SendMessageResponse
    {
        [JsonPropertyName("errcode")]
        public long Errcode { get; set; }

        [JsonPropertyName("errmsg")]
        public string Errmsg { get; set; }

        [JsonPropertyName("invaliduser")]
        public string Invaliduser { get; set; }

        [JsonPropertyName("invalidparty")]
        public string Invalidparty { get; set; }

        [JsonPropertyName("invalidtag")]
        public string Invalidtag { get; set; }

        [JsonPropertyName("unlicenseduser")]
        public string Unlicenseduser { get; set; }

        [JsonPropertyName("msgid")]
        public string Msgid { get; set; }

        [JsonPropertyName("response_code")]
        public string ResponseCode { get; set; }
    }
}
