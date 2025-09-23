namespace DreamSlave.Wecom.Attributes
{
    /// <summary>
    /// 企业微信文本特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class WecomTextCommandAttribute : Attribute
    {
        /// <summary>
        /// 命令
        /// </summary>
        public string[] Commands;

        public WecomTextCommandAttribute(params string[] command)
        {
            Commands = command;
        }
    }
}
