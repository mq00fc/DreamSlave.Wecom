namespace DreamSlave.Wecom.Attributes
{
    /// <summary>
    /// ��ҵ΢�Żص�����������
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class WecomHandlerScopeAttribute : Attribute
    {
        /// <summary>
        /// ����
        /// </summary>
        public string[] Names;

        public WecomHandlerScopeAttribute(params string[] names)
        {
            Names = names;
        }
    }
}
