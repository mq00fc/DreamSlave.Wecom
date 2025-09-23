namespace DreamSlave.Wecom.Hosts
{
    public class WecomRefreshHostService : BackgroundService
    {
        private readonly ILogger<WecomRefreshHostService> _logger;
        private readonly IWecomOAuth2Service _wecomOAuth2Service;
        private readonly IOptions<Models.Config> _options;
        public WecomRefreshHostService(
            ILogger<WecomRefreshHostService> logger,
            IWecomOAuth2Service wecomOAuth2Service,
            IOptions<Models.Config> options)
        {
            _logger = logger;
            _wecomOAuth2Service = wecomOAuth2Service;
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //如果未使用自动刷新则直接退出由使用者手动刷新
            if (!_options.Value.AutoRefresh)
            {
                return;
            }

            var retryDelay = TimeSpan.FromSeconds(10);
            var maxRetryDelay = TimeSpan.FromMinutes(5);
            var safety = TimeSpan.FromMinutes(5);
            var maxInterval = TimeSpan.FromMinutes(60); // 限制最长等待（可按需调）

            _logger.LogInformation("开始执行企业微信 token/ticket 刷新后台任务");

            // 启动立即尝试一次（避免第一次用户请求时还没有 token）
            try
            {
                var firstToken = await _wecomOAuth2Service.RefreshAccessTokenAsync();
                if (firstToken == null)
                {
                    _logger.LogWarning("启动时首次刷新 AccessToken 失败，将按重试策略继续。");
                }
                else
                {
                    var firstTicket = await _wecomOAuth2Service.RefreshJsApiTicketAsync();
                    if (firstTicket == null)
                        _logger.LogWarning("启动时首次刷新 jsapi-ticket 失败，将按重试策略继续。");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动时刷新出现异常（忽略，进入循环重试）。");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan nextDelay;
                try
                {
                    var accessToken = await _wecomOAuth2Service.RefreshAccessTokenAsync();
                    if (accessToken == null || accessToken.Errcode != 0 || string.IsNullOrEmpty(accessToken.AccessToken))
                    {
                        _logger.LogWarning("本轮 AccessToken 刷新失败，{RetrySeconds}s 后重试。", retryDelay.TotalSeconds);
                        nextDelay = retryDelay;
                        retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                        goto Delay;
                    }

                    var jsapiTicket = await _wecomOAuth2Service.RefreshJsApiTicketAsync();
                    if (jsapiTicket == null || jsapiTicket.Errcode != 0 || string.IsNullOrEmpty(jsapiTicket.Ticket))
                    {
                        _logger.LogWarning("本轮 jsapi-ticket 刷新失败，仅 ticket 将在 {RetrySeconds}s 后重试。", retryDelay.TotalSeconds);
                        nextDelay = retryDelay;
                        retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                        goto Delay;
                    }

                    // 计算最小过期
                    var accessExpire = TimeSpan.FromSeconds(Math.Max(accessToken.ExpiresIn, 60));
                    var ticketExpire = TimeSpan.FromSeconds(Math.Max(jsapiTicket.ExpiresIn, 60));
                    var minExpire = accessExpire < ticketExpire ? accessExpire : ticketExpire;

                    nextDelay = minExpire > safety ? minExpire - safety : TimeSpan.FromMinutes(1);
                    if (nextDelay > maxInterval) nextDelay = maxInterval;

                    retryDelay = TimeSpan.FromSeconds(10); // 重置退避
                    _logger.LogInformation("刷新成功，下次预计 {Minutes} 分钟后执行 (rawMin={RawMin}s)。", 
                        Math.Round(nextDelay.TotalMinutes, 2), (int)minExpire.TotalSeconds);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "刷新出现异常，将在 {RetrySeconds}s 后重试。", retryDelay.TotalSeconds);
                    nextDelay = retryDelay;
                    retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                }

            Delay:
                if (nextDelay <= TimeSpan.Zero)
                    nextDelay = TimeSpan.FromMinutes(1);

                try
                {
                    await Task.Delay(nextDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("WeCom 刷新后台任务已停止。");
        }
    }
}
