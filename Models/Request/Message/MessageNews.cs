namespace DreamSlave.Wecom.Models.Request.Message
{
    public class MessageNews
    {
        [JsonPropertyName("articles")]
        public List<MessageArticles> Articles { get; set; }
    }
}
