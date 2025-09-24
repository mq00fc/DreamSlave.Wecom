namespace DreamSlave.Wecom.Hosts
{
    internal interface IWecomInstanceRegistration
    {
        string Name { get; }
        bool AutoRefresh { get; }
    }

    internal sealed class WecomInstanceRegistration : IWecomInstanceRegistration
    {
        public string Name { get; }
        public bool AutoRefresh { get; }
        public WecomInstanceRegistration(string name, bool autoRefresh)
        {
            Name = name;
            AutoRefresh = autoRefresh;
        }
    }

    internal interface IWecomNamed<out T>
    {
        string Name { get; }
        T Service { get; }
    }
    internal sealed class WecomNamed<T> : IWecomNamed<T>
    {
        public string Name { get; }
        public T Service { get; }
        public WecomNamed(string name, T service)
        {
            Name = name;
            Service = service;
        }
    }
}
