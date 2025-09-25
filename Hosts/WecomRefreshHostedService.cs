namespace DreamSlave.Wecom.Hosts
{
    /// <summary>
    /// 统一管理多个企业微信实例刷新任务（基于 keyed 服务动态解析）
    /// </summary>
    internal sealed class WecomRefreshHostedService : BackgroundService
    {
        private readonly ILogger<WecomRefreshHostedService> _logger;
        private readonly IWecomUnifiedService _unified;
        private readonly ConcurrentDictionary<string, Task> _running = new(StringComparer.OrdinalIgnoreCase);

        public WecomRefreshHostedService(ILogger<WecomRefreshHostedService> logger,
            IWecomUnifiedService unified)
        {
            _logger = logger;
            _unified = unified;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 旧的 ServiceCollectionExtensions.GetRegistrations 仍返回 AutoRefresh 信息
            var regs = ServiceCollectionExtensions.GetRegistrations().ToList();
            if (regs.Count == 0)
            {
                _logger.LogInformation("[multi] 未发现任何 WeCom 实例，刷新服务空闲");
                return Task.CompletedTask;
            }
            foreach (var reg in regs)
            {
                if (!reg.AutoRefresh)
                {
                    _logger.LogInformation("[multi:{Name}] AutoRefresh=false 跳过", reg.Name);
                    continue;
                }
                _running[reg.Name] = Task.Run(() => LoopAsync(reg.Name, stoppingToken), stoppingToken);
            }
            return Task.WhenAll(_running.Values);
        }

        private async Task LoopAsync(string serviceName, CancellationToken token)
        {
            var cfg = _unified.GetConfig(serviceName);
            if (cfg == null)
            {
                _logger.LogWarning("[multi:{Name}] 未找到配置，退出", serviceName);
                return;
            }
            var safety = TimeSpan.FromMinutes(5);
            var retryDelay = TimeSpan.FromSeconds(10);
            var maxRetryDelay = TimeSpan.FromMinutes(5);
            var maxInterval = TimeSpan.FromHours(1);
            _logger.LogInformation("[multi:{Name}] 刷新循环启动 (CorpID={CorpID}, AgentId={AgentId})", serviceName, cfg.CorpID, cfg.AgentId);

            try
            {
                var t = await _unified.RefreshAccessTokenAsync(serviceName);
                if (t == null)
                    _logger.LogWarning("[multi:{Name}] 预热 AccessToken 失败", serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[multi:{Name}] 预热异常", serviceName);
            }

            while (!token.IsCancellationRequested)
            {
                TimeSpan nextDelay;
                try
                {
                    var accessToken = await _unified.RefreshAccessTokenAsync(serviceName);
                    if (accessToken == null || accessToken.Errcode != 0 || string.IsNullOrEmpty(accessToken.AccessToken))
                    {
                        _logger.LogWarning("[multi:{Name}] AccessToken 刷新失败，{Delay}s 后重试", serviceName, retryDelay.TotalSeconds);
                        nextDelay = retryDelay;
                        retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                        goto Delay;
                    }
                    var accessExpire = TimeSpan.FromSeconds(Math.Max(accessToken.ExpiresIn, 60));
                    nextDelay = accessExpire > safety ? accessExpire - safety : TimeSpan.FromMinutes(1);
                    if (nextDelay > maxInterval) nextDelay = maxInterval;
                    retryDelay = TimeSpan.FromSeconds(10);
                    _logger.LogInformation("[multi:{Name}] 刷新成功，下次 {Minutes} 分钟后 (rawMin={RawMin}s)", serviceName, Math.Round(nextDelay.TotalMinutes, 2), (int)accessExpire.TotalSeconds);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[multi:{Name}] 刷新异常，{Delay}s 后重试", serviceName, retryDelay.TotalSeconds);
                    nextDelay = retryDelay;
                    retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                }
            Delay:
                if (nextDelay <= TimeSpan.Zero) nextDelay = TimeSpan.FromMinutes(1);
                try { await Task.Delay(nextDelay, token); } catch { break; }
            }
            _logger.LogInformation("[multi:{Name}] 刷新循环结束", serviceName);
        }
    }
}
