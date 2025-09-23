namespace DreamSlave.Wecom.Hosts
{
    /// <summary>
    /// 企业微信命令执行服务
    /// </summary>
    public class WecomCommandExecService : BackgroundService
    {
        private readonly ILogger<WecomCommandExecService> _logger;
        private readonly IServiceProvider _provider;
        private readonly IWecomCallBackService _wecomCallBackService;
        private readonly IOptions<Models.Config> _options;
        private readonly string _instanceName;

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
            IOptions<Models.Config> options,
            IServiceProvider provider,
            IWecomCallBackService wecomCallBackService,
            string instanceName)
        {
            _logger = logger;
            _options = options;
            _provider = provider;
            _wecomCallBackService = wecomCallBackService;

            // 先初始化为空，避免并发调用未初始化导致空引用
            _textHandlers = new Dictionary<string, Func<IServiceProvider, MessageReceive, Task<string>>>(StringComparer.OrdinalIgnoreCase);
            _eventHandlers = new Dictionary<string, Func<IServiceProvider, MessageReceive, Task<string>>>(StringComparer.OrdinalIgnoreCase);
            _instanceName = instanceName ?? "default";
        }


        #region 私有方法
        /// <summary>
        /// 判断方法是否在当前实例作用域内
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private bool IsInScope(MethodInfo method)
        {
            // 方法级别
            var mScope = method.GetCustomAttribute<WecomHandlerScopeAttribute>();
            if (mScope != null)
            {
                // 未指定 name 视为全局
                if (mScope.Names == null || mScope.Names.Length == 0) return true;
                return mScope.Names.Any(n => string.Equals(n, _instanceName, StringComparison.OrdinalIgnoreCase));
            }

            // 类级别
            var tScope = method.DeclaringType?.GetCustomAttribute<WecomHandlerScopeAttribute>();
            if (tScope != null)
            {
                if (tScope.Names == null || tScope.Names.Length == 0) return true;
                return tScope.Names.Any(n => string.Equals(n, _instanceName, StringComparison.OrdinalIgnoreCase));
            }

            // 未标注，默认在所有实例生效
            return true;
        }

        /// <summary>
        /// 注册处理器
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="methods"></param>
        /// <param name="handlers"></param>
        /// <param name="commandsSelector"></param>
        /// <returns></returns>
        private void RegisterHandlers<TAttribute>(
            IEnumerable<MethodInfo> methods,
            Dictionary<string, Func<IServiceProvider, MessageReceive, Task<string>>> handlers,
            Func<TAttribute, IEnumerable<string>> commandsSelector)
            where TAttribute : Attribute
        {
            foreach (var method in methods)
            {
                var commandAttribute = method.GetCustomAttribute<TAttribute>();
                if (commandAttribute is null) continue;

                var commands = commandsSelector(commandAttribute);
                if (commands is null) continue;

                // 依据作用域过滤
                if (!IsInScope(method)) continue;

                foreach (var item in commands)
                {
                    var command = item?.Trim().ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(command)) continue;

                    var targetType = method.DeclaringType!;
                    object targetInstance;
                    try
                    {
                        targetInstance = ActivatorUtilities.CreateInstance(_provider, targetType);
                    }
                    catch
                    {
                        targetInstance = Activator.CreateInstance(targetType)!;
                    }

                    var handler = (Func<IServiceProvider, MessageReceive, Task<string>>)method.CreateDelegate(
                        typeof(Func<IServiceProvider, MessageReceive, Task<string>>), targetInstance);

                    handlers[command] = handler;
                }
            }
        }

        /// <summary>
        /// 安全获取可加载类型，避免 ReflectionTypeLoadException
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.OfType<Type>();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }
        #endregion


        /// <summary>
        /// 执行（启动时预热注册）
        /// </summary>
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

                // 支持外部程序集中的命令处理器
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var methods = assemblies
                    .SelectMany(GetLoadableTypes)
                    .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    .ToArray();

                // 清空并重建字典
                _textHandlers.Clear();
                _eventHandlers.Clear();

                // 统一注册文本命令处理器
                RegisterHandlers<WecomTextCommandAttribute>(methods, _textHandlers, attr => attr.Commands);
                // 统一注册事件命令处理器
                RegisterHandlers<WecomEventCommandAttribute>(methods, _eventHandlers, attr => new string[] { attr.Name });

                _logger.LogInformation("实例:{0},文本命令{1}个，事件命令{2}个", _instanceName, _textHandlers.Count, _eventHandlers.Count);
                _initialized = true;
            }
        }

        /// <summary>
        /// 文本消息处理
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task<string> HandleMessageAsync(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return string.Empty;

            // 懒加载，避免首次请求早于后台任务启动时发生空引用
            TryInitializeHandlers();

            var model = _wecomCallBackService.ResolvePayload(payload);
            if (model is null)
                return "";

            //如果不是文本的话,则走其他的事件回调!
            if (string.Equals(model.MsgType, "event", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleEventAsync(model, payload);
            }

            //无法处理的消息类型
            if (!string.Equals(model.MsgType, "text", StringComparison.OrdinalIgnoreCase))
            {
                return HandleUnknowCommand(model, payload);
            }

            try
            {
                var key = (model.Content ?? string.Empty).Trim().ToLowerInvariant();
                //没有找到指定的命令则返回未知命令
                if (!_textHandlers.TryGetValue(key, out var handler))
                {
                    return HandleUnknowCommand(model, payload);
                }

                var repose = await handler.Invoke(_provider, model).ConfigureAwait(false);
                return _wecomCallBackService.SendTextMessage(model.fromUserName, repose);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex);
                return _wecomCallBackService.SendTextMessage(model.fromUserName, "消息解析异常,请联系开发者!");
            }
        }

        /// <summary>
        /// 处理事件消息
        /// </summary>
        /// <param name="messageReceive"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private async Task<string> HandleEventAsync(MessageReceive messageReceive, string payload)
        {
            try
            {
                var key = (messageReceive.EventName ?? string.Empty).Trim().ToLowerInvariant();
                //没有找到指定的命令则返回未知命令
                if (!_eventHandlers.TryGetValue(key, out var handler))
                {
                    return HandleUnknowCommand(messageReceive, payload);
                }

                var repose = await handler.Invoke(_provider, messageReceive).ConfigureAwait(false);
                return _wecomCallBackService.SendTextMessage(messageReceive.fromUserName, repose);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex);
                return _wecomCallBackService.SendTextMessage(messageReceive.fromUserName, "消息解析异常,请联系开发者!");
            }
        }


        /// <summary>
        /// 处理错误的消息
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        private Task HandleErrorAsync(Exception exception)
        {
            _logger.LogError("实例:{0} 出现错误:{1}\n{2}", _instanceName,exception.Message, exception.StackTrace);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 处理未知的命令
        /// </summary>
        /// <param name="messageReceive"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private string HandleUnknowCommand(MessageReceive messageReceive, string payload)
        {
            _logger.LogWarning("实例:{0},以下消息无法被自动识别:{1}\n{2}",_instanceName,messageReceive.MsgType, payload);
            return _wecomCallBackService.SendTextMessage(messageReceive.fromUserName, "我还不理解您的意思,消息已记录并等待专员回复!");
        }
    }
}
