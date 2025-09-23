namespace DreamSlave.Wecom.Services
{
    internal sealed class WecomFactory : IWecomFactory
    {
        private readonly IServiceProvider _sp;
        public WecomFactory(IServiceProvider sp)
        {
            _sp = sp;
        }

        public IWecomOAuth2Service GetOAuth2(string name)
            => _sp.GetRequiredKeyedService<IWecomOAuth2Service>(name);

        public IWecomCallBackService GetCallback(string name)
            => _sp.GetRequiredKeyedService<IWecomCallBackService>(name);
    }
}
