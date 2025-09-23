namespace DreamSlave.Wecom
{
    public static class ServiceCollectionExtensions
    {
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
            //执行自动刷新
            services.AddHostedService<WecomRefreshHostService>();

            services.TryAddSingleton<IWecomFactory, WecomFactory>();
            return services;
        }

        /// <summary>
        /// 支持多实例（类似 HttpClientFactory 的命名用法），可通过名称区分不同的企业微信配置。
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

            services.TryAddSingleton<IWecomFactory, WecomFactory>();
            return services;
        }
    }
}
