namespace DreamSlave.Wecom.Attributes
{
    /// <summary>
    /// 企业微信事件特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class WecomEventCommandAttribute : Attribute
    {
        /// <summary>
        /// 企业微信事件key
        /// </summary>
        public string Name { get; set; }
        public WecomEventCommandAttribute(string name)
        {
            Name = name;
        }
    }
}
