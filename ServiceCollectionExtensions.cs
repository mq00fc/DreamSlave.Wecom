namespace DreamSlave.Wecom
{
    public static class ServiceCollectionExtensions
    {
        private static readonly object _initLock = new();
        private static bool _multiHostAdded;
        private static bool _clientAdded;
        private static readonly ConcurrentDictionary<string, bool> _registrations = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, string> _botKeys = new(StringComparer.OrdinalIgnoreCase);

        internal static IEnumerable<(string Name, bool AutoRefresh)> GetRegistrations()
            => _registrations.Select(kv => (kv.Key, kv.Value));

        private static void EnsureWecomHttpClient(IServiceCollection services)
        {
            if (_clientAdded) return;
            lock (_initLock)
            {
                if (_clientAdded) return;
                services.AddHttpClient("wecom_client", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    client.DefaultRequestHeaders.Add("User-Agent", "DreamSlave.Wecom");
                }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    UseCookies = false,
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.All,
                    MaxResponseHeadersLength = 1024,
#if !NET8_0_OR_GREATER && !NET9_0_OR_GREATER
                    // 旧框架兼容属性可能不存在
#else
                    MaxRequestContentBufferSize = 1024,
#endif
                    CheckCertificateRevocationList = true,
                    MaxConnectionsPerServer = 500,
                });
                _clientAdded = true;
            }
        }

        private static void EnsureMultiHost(IServiceCollection services)
        {
            if (_multiHostAdded) return;
            lock (_initLock)
            {
                if (_multiHostAdded) return;
                services.AddHostedService<WecomRefreshHostedService>();
                _multiHostAdded = true;
            }
        }

        // 新的轻量注册：仅向统一服务添加配置，不再为每个实例创建一组 service 对象
        public static IServiceCollection AddWecomConfig(this IServiceCollection services, string serviceName, Action<Models.Config> configure)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) serviceName = "default";
            var cfg = new Models.Config();
            configure(cfg);
            DreamSlave.Wecom.Services.WecomUnifiedService.RegisterConfig(serviceName, cfg);
            _registrations[serviceName] = cfg.AutoRefresh;

            // 基础依赖只注册一次
            services.AddMemoryCache();
            EnsureWecomHttpClient(services);
            EnsureMultiHost(services);

            // 统一服务单例（线程安全，多租户）
            services.TryAddSingleton<IWecomUnifiedService, WecomUnifiedService>();

            // Keyed 注册命令执行服务，供回调控制器解析
            services.AddKeyedSingleton<Hosts.WecomCommandExecService>(serviceName, (sp, key) =>
                new Hosts.WecomCommandExecService(
                    sp.GetRequiredService<ILogger<Hosts.WecomCommandExecService>>(),
                    sp.GetRequiredService<IWecomUnifiedService>(),
                        sp,
                    serviceName));
            // 作为 IHostedService 启动
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredKeyedService<Hosts.WecomCommandExecService>(serviceName));

            return services;
        }

        public static IServiceCollection AddWecomBot(this IServiceCollection services, string botName, string key)
        {
            if (string.IsNullOrWhiteSpace(botName)) botName = "default";
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key empty", nameof(key));
            EnsureWecomHttpClient(services);
            _botKeys[botName] = key;
            services.AddKeyedSingleton<IWecomBotService>(botName, (sp, k) => new WecomBotService(
                sp.GetRequiredService<ILogger<WecomBotService>>(),
                sp.GetRequiredService<IHttpClientFactory>(), botName, key));
            // 默认桥接
            if (botName.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                services.TryAddSingleton<IWecomBotService>(sp => sp.GetRequiredKeyedService<IWecomBotService>("default"));
            }
            return services;
        }
    }
}
