using System.Collections.Concurrent;

namespace DreamSlave.Wecom.Hosts
{
    /// <summary>
    /// 统一管理多个企业微信实例刷新任务（基于 keyed 服务动态解析）
    /// </summary>
    internal sealed class MultiWecomRefreshHostedService : BackgroundService
    {
        private readonly ILogger<MultiWecomRefreshHostedService> _logger;
        private readonly IServiceProvider _sp;
        private readonly ConcurrentDictionary<string, Task> _runningTasks = new(StringComparer.OrdinalIgnoreCase);

        public MultiWecomRefreshHostedService(
            ILogger<MultiWecomRefreshHostedService> logger,
            IServiceProvider sp)
        {
            _logger = logger;
            _sp = sp;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 从 ServiceCollectionExtensions 的静态注册表获取实例信息
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
                var task = Task.Run(() => RunInstanceLoopAsync(reg.Name, stoppingToken), stoppingToken);
                _runningTasks[reg.Name] = task;
            }
            return Task.WhenAll(_runningTasks.Values);
        }

        private async Task RunInstanceLoopAsync(string name, CancellationToken token)
        {
            IWecomOAuth2Service oauth;
            try
            {
                oauth = _sp.GetRequiredKeyedService<IWecomOAuth2Service>(name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[multi:{Name}] 未找到对应的 OAuth2 服务，退出刷新循环", name);
                return;
            }

            var config = oauth.GetConfig();
            var safety = TimeSpan.FromMinutes(5);
            var retryDelay = TimeSpan.FromSeconds(10);
            var maxRetryDelay = TimeSpan.FromMinutes(5);
            var maxInterval = TimeSpan.FromHours(1);
            _logger.LogInformation("[multi:{Name}] 刷新循环启动 (CorpID={CorpID}, AgentId={AgentId})", name, config.CorpID, config.AgentId);

            // 立即预热
            try
            {
                var t = await oauth.RefreshAccessTokenAsync();
                if (t == null)
                    _logger.LogWarning("[multi:{Name}] 预热 AccessToken 失败", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[multi:{Name}] 预热异常", name);
            }

            while (!token.IsCancellationRequested)
            {
                TimeSpan nextDelay;
                try
                {
                    var accessToken = await oauth.RefreshAccessTokenAsync();
                    if (accessToken == null || accessToken.Errcode != 0 || string.IsNullOrEmpty(accessToken.AccessToken))
                    {
                        _logger.LogWarning("[multi:{Name}] AccessToken 刷新失败，{Delay}s 后重试", name, retryDelay.TotalSeconds);
                        nextDelay = retryDelay;
                        retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                        goto Delay;
                    }

                    var accessExpire = TimeSpan.FromSeconds(Math.Max(accessToken.ExpiresIn, 60));
                    nextDelay = accessExpire > safety ? accessExpire - safety : TimeSpan.FromMinutes(1);
                    if (nextDelay > maxInterval) nextDelay = maxInterval;
                    retryDelay = TimeSpan.FromSeconds(10);
                    _logger.LogInformation("[multi:{Name}] 刷新成功，下次 {Minutes} 分钟后 (rawMin={RawMin}s)", name, Math.Round(nextDelay.TotalMinutes, 2), (int)accessExpire.TotalSeconds);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[multi:{Name}] 刷新异常，{Delay}s 后重试", name, retryDelay.TotalSeconds);
                    nextDelay = retryDelay;
                    retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                }
            Delay:
                if (nextDelay <= TimeSpan.Zero) nextDelay = TimeSpan.FromMinutes(1);
                try { await Task.Delay(nextDelay, token); } catch { break; }
            }
            _logger.LogInformation("[multi:{Name}] 刷新循环结束", name);
        }
    }
}
