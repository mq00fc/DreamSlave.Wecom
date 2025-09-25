namespace DreamSlave.Wecom.Hosts
{
    /// <summary>
    /// 企业微信命令执行服务
    /// </summary>
    public class WecomCommandExecService : BackgroundService
    {
        private readonly ILogger<WecomCommandExecService> _logger;
        private readonly IServiceProvider _provider;
        private readonly IWecomUnifiedService _unified;
        private readonly string _serviceName;

        private Dictionary<string, Func<IServiceProvider, MessageReceive, Task<string>>> _textHandlers;
        private Dictionary<string, Func<IServiceProvider, MessageReceive, Task<string>>> _eventHandlers;
        private volatile bool _initialized;
#if NET9_0_OR_GREATER
        private readonly Lock _initLock = new();
#else
        private readonly object _initLock = new object();
#endif
        public WecomCommandExecService(
            ILogger<WecomCommandExecService> logger,
            IWecomUnifiedService unified,
            IServiceProvider provider,
            string serviceName)
        {
            _logger = logger;
            _provider = provider;
            _unified = unified;
            _serviceName = string.IsNullOrWhiteSpace(serviceName) ? "default" : serviceName;
            _textHandlers = new(StringComparer.OrdinalIgnoreCase);
            _eventHandlers = new(StringComparer.OrdinalIgnoreCase);
        }

        private bool IsInScope(MethodInfo method)
        {
            var mScope = method.GetCustomAttribute<WecomHandlerScopeAttribute>();
            if (mScope != null)
            {
                if (mScope.Names == null || mScope.Names.Length == 0) return true;
                return mScope.Names.Any(n => string.Equals(n, _serviceName, StringComparison.OrdinalIgnoreCase));
            }
            var tScope = method.DeclaringType?.GetCustomAttribute<WecomHandlerScopeAttribute>();
            if (tScope != null)
            {
                if (tScope.Names == null || tScope.Names.Length == 0) return true;
                return tScope.Names.Any(n => string.Equals(n, _serviceName, StringComparison.OrdinalIgnoreCase));
            }
            return true;
        }

        private void RegisterHandlers<TAttribute>(IEnumerable<MethodInfo> methods,
            Dictionary<string, Func<IServiceProvider, MessageReceive, Task<string>>> handlers,
            Func<TAttribute, IEnumerable<string>> commandsSelector) where TAttribute : Attribute
        {
            foreach (var method in methods)
            {
                var commandAttribute = method.GetCustomAttribute<TAttribute>();
                if (commandAttribute is null) continue;
                var commands = commandsSelector(commandAttribute);
                if (commands is null) continue;
                if (!IsInScope(method)) continue;
                foreach (var item in commands)
                {
                    var command = item?.Trim().ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(command)) continue;
                    var targetType = method.DeclaringType!;
                    object targetInstance; try { targetInstance = ActivatorUtilities.CreateInstance(_provider, targetType); } catch { targetInstance = Activator.CreateInstance(targetType)!; }
                    var handler = (Func<IServiceProvider, MessageReceive, Task<string>>)method.CreateDelegate(typeof(Func<IServiceProvider, MessageReceive, Task<string>>), targetInstance);
                    handlers[command] = handler;
                }
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            catch { return Array.Empty<Type>(); }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TryInitializeHandlers();
            return Task.CompletedTask;
        }

        private void TryInitializeHandlers()
        {
            if (_initialized) return;
            lock (_initLock)
            {
                if (_initialized) return;
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var methods = assemblies.SelectMany(GetLoadableTypes)
                    .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    .ToArray();
                _textHandlers.Clear();
                _eventHandlers.Clear();
                RegisterHandlers<WecomTextCommandAttribute>(methods, _textHandlers, attr => attr.Commands);
                RegisterHandlers<WecomEventCommandAttribute>(methods, _eventHandlers, attr => new string[] { attr.Name });
                _logger.LogInformation("实例:{0},文本命令{1}个，事件命令{2}个", _serviceName, _textHandlers.Count, _eventHandlers.Count);
                _initialized = true;
            }
        }

        public async Task<string> HandleMessageAsync(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return string.Empty;
            TryInitializeHandlers();
            var model = _unified.ResolvePayload(payload);
            if (model is null) return string.Empty;
            if (string.Equals(model.MsgType, "event", StringComparison.OrdinalIgnoreCase))
                return await HandleEventAsync(model, payload);
            if (!string.Equals(model.MsgType, "text", StringComparison.OrdinalIgnoreCase))
                return HandleUnknowCommand(model, payload);
            try
            {
                var key = (model.Content ?? string.Empty).Trim().ToLowerInvariant();
                if (!_textHandlers.TryGetValue(key, out var handler))
                    return HandleUnknowCommand(model, payload);
                var response = await handler.Invoke(_provider, model).ConfigureAwait(false);
                return _unified.SendCallbackTextMessage(_serviceName, model.fromUserName, response);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex);
                return _unified.SendCallbackTextMessage(_serviceName, model.fromUserName, "消息解析异常,请联系开发者!");
            }
        }

        private async Task<string> HandleEventAsync(MessageReceive messageReceive, string payload)
        {
            try
            {
                var key = (messageReceive.EventName ?? string.Empty).Trim().ToLowerInvariant();
                if (!_eventHandlers.TryGetValue(key, out var handler))
                    return HandleUnknowCommand(messageReceive, payload);
                var response = await handler.Invoke(_provider, messageReceive).ConfigureAwait(false);
                return _unified.SendCallbackTextMessage(_serviceName, messageReceive.fromUserName, response);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex);
                return _unified.SendCallbackTextMessage(_serviceName, messageReceive.fromUserName, "消息解析异常,请联系开发者!");
            }
        }

        private Task HandleErrorAsync(Exception exception)
        {
            _logger.LogError("实例:{0} 出现错误:{1}\n{2}", _serviceName, exception.Message, exception.StackTrace);
            return Task.CompletedTask;
        }
        private string HandleUnknowCommand(MessageReceive messageReceive, string payload)
        {
            _logger.LogWarning("实例:{0},以下消息无法被自动识别:{1}\n{2}", _serviceName, messageReceive.MsgType, payload);
            return _unified.SendCallbackTextMessage(_serviceName, messageReceive.fromUserName, "我还不理解您的意思,消息已记录并等待专员回复!");
        }
    }
}
