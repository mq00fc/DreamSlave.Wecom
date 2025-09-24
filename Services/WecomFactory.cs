namespace DreamSlave.Wecom.Services
{
    internal sealed class WecomFactory : IWecomFactory
    {
        private readonly IServiceProvider _sp;
        public WecomFactory(IServiceProvider sp)
        {
            _sp = sp;
        }

        public IWecomOAuth2Service GetOAuth2(string name) => _sp.GetRequiredKeyedService<IWecomOAuth2Service>(name);
        public IWecomCallBackService GetCallback(string name) => _sp.GetRequiredKeyedService<IWecomCallBackService>(name);
        public IWecomBotService GetBotService(string name) => _sp.GetRequiredKeyedService<IWecomBotService>(name);
        public IWecomMessageService GetMessageService(string name) => _sp.GetRequiredKeyedService<IWecomMessageService>(name);
        public WecomCommandExecService GetCommandExec(string name) => _sp.GetRequiredKeyedService<WecomCommandExecService>(name);
    }
}
