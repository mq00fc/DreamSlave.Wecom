namespace DreamSlave.Wecom
{
    public static class ServiceCollectionExtensions
    {
        private static readonly object _initLock = new();
        private static bool _multiHostAdded;
        private static bool _clientAdded;
        // 仅保存注册名称及其 AutoRefresh 标志，实例由 DI 延迟创建
        private static readonly ConcurrentDictionary<string, bool> _registrations = new(StringComparer.OrdinalIgnoreCase);

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
                services.AddHostedService<DreamSlave.Wecom.Hosts.MultiWecomRefreshHostedService>();
                _multiHostAdded = true;
            }
        }

        public static IServiceCollection AddWecomService(this IServiceCollection services, Action<Models.Config> configure)
            => AddWecomService(services, "default", configure);

        public static IServiceCollection AddWecomService(this IServiceCollection services, string name, Action<Models.Config> configure)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name cannot be null or whitespace", nameof(name));

            services.AddMemoryCache();
            services.AddOptions<Models.Config>(name).Configure(configure).ValidateOnStart();
            var cfgTmp = new Models.Config();
            configure(cfgTmp);
            _registrations[name] = cfgTmp.AutoRefresh;

            EnsureWecomHttpClient(services);
            EnsureMultiHost(services);

            services.AddKeyedSingleton<IWecomOAuth2Service>(name, (sp, key) =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomOAuth2Service>>();
                var http = sp.GetRequiredService<IHttpClientFactory>();
                var cache = sp.GetRequiredService<IMemoryCache>();
                var optMon = sp.GetRequiredService<IOptionsMonitor<Models.Config>>();
                return new WecomOAuth2Service(logger, http, Options.Create(optMon.Get(name)), cache);
            });
            services.AddSingleton<DreamSlave.Wecom.Hosts.IWecomNamed<IWecomOAuth2Service>>(sp => new DreamSlave.Wecom.Hosts.WecomNamed<IWecomOAuth2Service>(name, sp.GetRequiredKeyedService<IWecomOAuth2Service>(name)));

            services.AddKeyedSingleton<IWecomCallBackService>(name, (sp, key) =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomCallBackService>>();
                var http = sp.GetRequiredService<IHttpClientFactory>();
                var optMon = sp.GetRequiredService<IOptionsMonitor<Models.Config>>();
                return new WecomCallBackService(logger, http, Options.Create(optMon.Get(name)));
            });
            services.AddSingleton<DreamSlave.Wecom.Hosts.IWecomNamed<IWecomCallBackService>>(sp => new DreamSlave.Wecom.Hosts.WecomNamed<IWecomCallBackService>(name, sp.GetRequiredKeyedService<IWecomCallBackService>(name)));

            services.AddKeyedSingleton<IWecomMessageService>(name, (sp, key) =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomMessageService>>();
                var oauth = sp.GetRequiredKeyedService<IWecomOAuth2Service>(name);
                var http = sp.GetRequiredService<IHttpClientFactory>();
                return new WecomMessageService(logger, http, oauth);
            });
            services.AddSingleton<DreamSlave.Wecom.Hosts.IWecomNamed<IWecomMessageService>>(sp => new DreamSlave.Wecom.Hosts.WecomNamed<IWecomMessageService>(name, sp.GetRequiredKeyedService<IWecomMessageService>(name)));

            services.AddKeyedSingleton<WecomCommandExecService>(name, (sp, key) =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomCommandExecService>>();
                var optMon = sp.GetRequiredService<IOptionsMonitor<Models.Config>>();
                var cb = sp.GetRequiredKeyedService<IWecomCallBackService>(name);
                return new WecomCommandExecService(logger, Options.Create(optMon.Get(name)), sp, cb, name);
            });
            services.AddSingleton<DreamSlave.Wecom.Hosts.IWecomNamed<WecomCommandExecService>>(sp => new DreamSlave.Wecom.Hosts.WecomNamed<WecomCommandExecService>(name, sp.GetRequiredKeyedService<WecomCommandExecService>(name)));

            // 显式作为 IHostedService 注册，避免委托扩展可能的合并问题
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredKeyedService<WecomCommandExecService>(name));

            services.TryAddSingleton<IWecomFactory, WecomFactory>();
            return services;
        }

        public static IServiceCollection AddWecomBot(this IServiceCollection services, string botName, string botKey)
        {
            if (string.IsNullOrWhiteSpace(botKey)) throw new ArgumentException("botKey cannot be null or whitespace", nameof(botKey));
            if (string.IsNullOrWhiteSpace(botName)) botName = "default";

            EnsureWecomHttpClient(services);

            services.AddKeyedSingleton<IWecomBotService>(botName, (sp, key) =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomBotService>>();
                var http = sp.GetRequiredService<IHttpClientFactory>();
                return new WecomBotService(logger, http, botName, botKey);
            });
            services.AddSingleton<DreamSlave.Wecom.Hosts.IWecomNamed<IWecomBotService>>(sp => new DreamSlave.Wecom.Hosts.WecomNamed<IWecomBotService>(botName, sp.GetRequiredKeyedService<IWecomBotService>(botName)));

            services.TryAddSingleton<IWecomFactory, WecomFactory>();
            return services;
        }
    }
}
