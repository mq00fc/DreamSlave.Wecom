namespace DreamSlave.Wecom.Attributes
{
    /// <summary>
    /// 企业微信回调作用域特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class WecomHandlerScopeAttribute : Attribute
    {
        /// <summary>
        /// 命令
        /// </summary>
        public string[] Names;

        public WecomHandlerScopeAttribute(params string[] names)
        {
            Names = names;
        }
    }
}
