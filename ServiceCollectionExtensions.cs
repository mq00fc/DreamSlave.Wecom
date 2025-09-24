namespace DreamSlave.Wecom
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注入企业微信服务，单实例模式
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollection AddWecomService(this IServiceCollection services, Action<Models.Config> configure)
        {
            services.AddMemoryCache();

            services.AddOptions<Models.Config>()
                    .Configure(configure)
                    .ValidateOnStart();


            //http客户端
            services.AddHttpClient(name: "wecom_client", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(3);
                client.DefaultRequestHeaders.Add("User-Agent", "DreamSlave.Wecom");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = true,
                //自动解码非常必要
                AutomaticDecompression = DecompressionMethods.All,
                MaxResponseHeadersLength = 1024,
                MaxRequestContentBufferSize = 1024,
                CheckCertificateRevocationList = true,
                MaxConnectionsPerServer = 500,
            });

            services.AddSingleton<IWecomOAuth2Service, WecomOAuth2Service>();
            services.AddSingleton<IWecomCallBackService, WecomCallBackService>();
            services.AddSingleton<IWecomMessageService, WecomMessageService>();
            services.TryAddSingleton<IWecomFactory, WecomFactory>();

            //执行自动刷新
            services.AddHostedService<WecomRefreshHostService>();

            //注册单例服务并进行托管
            services.AddSingleton<WecomCommandExecService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomCommandExecService>>();
                var opts = sp.GetRequiredService<IOptions<Models.Config>>();
                var cb = sp.GetRequiredService<IWecomCallBackService>();
                return new WecomCommandExecService(logger, opts, sp, cb, "default");
            });

            return services;
        }

        /// <summary>
        /// 注入企业微信服务，支持多实例模式
        /// 用法：
        /// services.AddWecomService("appA", cfg => { ... });
        /// services.AddWecomService("appB", cfg => { ... });
        /// 注入：IWecomFactory.GetOAuth2("appA") / GetCallback("appA")，或 [FromKeyedServices("appA")] 按键注入。
        /// </summary>
        public static IServiceCollection AddWecomService(this IServiceCollection services, string name, Action<Models.Config> configure)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name cannot be null or whitespace", nameof(name));

            services.AddMemoryCache();

            // 为该名称注册独立的配置实例
            services.AddOptions<Models.Config>(name)
                    .Configure(configure)
                    .ValidateOnStart();

            // 从 configure 生成配置快照，用于条件注册
            var cfgSnapshot = new Models.Config();
            configure(cfgSnapshot);


            // 确保 HttpClient 存在（共享同一个 wecom_client）
            services.AddHttpClient(name: "wecom_client", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(3);
                client.DefaultRequestHeaders.Add("User-Agent", "DreamSlave.Wecom");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All,
                MaxResponseHeadersLength = 1024,
                MaxRequestContentBufferSize = 1024,
                CheckCertificateRevocationList = true,
                MaxConnectionsPerServer = 500,
            });

            // 使用 KeyedService 注册，便于按名称解析
            services.AddKeyedSingleton<IWecomOAuth2Service, WecomOAuth2Service>(name, (sp, _) =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomOAuth2Service>>();
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<Models.Config>>();
                var memoryCache = sp.GetRequiredService<IMemoryCache>();
                return new WecomOAuth2Service(
                    logger,
                    httpFactory,
                    Options.Create(optionsMonitor.Get(name)),
                    memoryCache);
            });

            services.AddKeyedSingleton<IWecomCallBackService, WecomCallBackService>(name, (sp, _) =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomCallBackService>>();
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<Models.Config>>();
                return new WecomCallBackService(
                    logger,
                    httpFactory,
                    Options.Create(optionsMonitor.Get(name)));
            });


            services.AddKeyedSingleton<IWecomMessageService, WecomMessageService>(name, (sp, _) =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomMessageService>>();
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var wecomFactory = sp.GetRequiredService<IWecomFactory>();
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<Models.Config>>();
                return new WecomMessageService(
                    logger,
                    sp,
                    name,
                    httpFactory);
            });


            services.TryAddSingleton<IWecomFactory, WecomFactory>();

            // 为该名称添加一个独立的后台刷新任务
            services.AddHostedService(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomRefreshHostService>>();
                var wecomSvc = sp.GetRequiredKeyedService<IWecomOAuth2Service>(name);
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<Models.Config>>();
                return new WecomRefreshHostService(
                    logger,
                    wecomSvc,
                    Options.Create(optionsMonitor.Get(name)));
            });


            // 注册命令执行服务（按 name 隔离）并进行托管
            services.AddKeyedSingleton<WecomCommandExecService>(name, (sp, _) =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomCommandExecService>>();
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<Models.Config>>();
                var callBackSvc = sp.GetRequiredKeyedService<IWecomCallBackService>(name);
                return new WecomCommandExecService(
                    logger,
                    Options.Create(optionsMonitor.Get(name)),
                    sp,
                    callBackSvc,
                    name);
            });
            services.AddHostedService(sp => sp.GetRequiredKeyedService<WecomCommandExecService>(name));
            return services;
        }


        /// <summary>
        /// 注入企业微信机器人服务，支持多实例模式
        /// </summary>
        /// <param name="services"></param>
        /// <param name="botName">机器人名称</param>
        /// <param name="botKey">机器人url(仅需要?key=后的值)</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IServiceCollection AddWecomBot(this IServiceCollection services, string botName, string botKey)
        {
            if (string.IsNullOrWhiteSpace(botKey)) throw new ArgumentException("botKey cannot be null or whitespace", nameof(botKey));
            if (string.IsNullOrWhiteSpace(botName)) botName = "default";
            // 确保 HttpClient 存在（共享同一个 wecom_bot）
            services.AddHttpClient(name: "wecom_bot", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(3);
                client.DefaultRequestHeaders.Add("User-Agent", "DreamSlave.Wecom");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All,
                MaxResponseHeadersLength = 1024,
                MaxRequestContentBufferSize = 1024,
                CheckCertificateRevocationList = true,
                MaxConnectionsPerServer = 500,
            });
            services.AddKeyedSingleton<IWecomBotService, WecomBotService>(botName, (sp, _) =>
            {
                var logger = sp.GetRequiredService<ILogger<WecomBotService>>();
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                return new WecomBotService(logger, httpFactory, botName, botKey);
            });
            return services;
        }
    }
}
