namespace DreamSlave.Wecom.Hosts
{
    /// <summary>
    /// ͳһ��������ҵ΢��ʵ��ˢ�����񣨻��� keyed ����̬������
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
            // �ɵ� ServiceCollectionExtensions.GetRegistrations �Է��� AutoRefresh ��Ϣ
            var regs = ServiceCollectionExtensions.GetRegistrations().ToList();
            if (regs.Count == 0)
            {
                _logger.LogInformation("[multi] δ�����κ� WeCom ʵ����ˢ�·������");
                return Task.CompletedTask;
            }
            foreach (var reg in regs)
            {
                if (!reg.AutoRefresh)
                {
                    _logger.LogInformation("[multi:{Name}] AutoRefresh=false ����", reg.Name);
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
                _logger.LogWarning("[multi:{Name}] δ�ҵ����ã��˳�", serviceName);
                return;
            }
            var safety = TimeSpan.FromMinutes(5);
            var retryDelay = TimeSpan.FromSeconds(10);
            var maxRetryDelay = TimeSpan.FromMinutes(5);
            var maxInterval = TimeSpan.FromHours(1);
            _logger.LogInformation("[multi:{Name}] ˢ��ѭ������ (CorpID={CorpID}, AgentId={AgentId})", serviceName, cfg.CorpID, cfg.AgentId);

            try
            {
                var t = await _unified.RefreshAccessTokenAsync(serviceName);
                if (t == null)
                    _logger.LogWarning("[multi:{Name}] Ԥ�� AccessToken ʧ��", serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[multi:{Name}] Ԥ���쳣", serviceName);
            }

            while (!token.IsCancellationRequested)
            {
                TimeSpan nextDelay;
                try
                {
                    var accessToken = await _unified.RefreshAccessTokenAsync(serviceName);
                    if (accessToken == null || accessToken.Errcode != 0 || string.IsNullOrEmpty(accessToken.AccessToken))
                    {
                        _logger.LogWarning("[multi:{Name}] AccessToken ˢ��ʧ�ܣ�{Delay}s ������", serviceName, retryDelay.TotalSeconds);
                        nextDelay = retryDelay;
                        retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                        goto Delay;
                    }
                    var accessExpire = TimeSpan.FromSeconds(Math.Max(accessToken.ExpiresIn, 60));
                    nextDelay = accessExpire > safety ? accessExpire - safety : TimeSpan.FromMinutes(1);
                    if (nextDelay > maxInterval) nextDelay = maxInterval;
                    retryDelay = TimeSpan.FromSeconds(10);
                    _logger.LogInformation("[multi:{Name}] ˢ�³ɹ����´� {Minutes} ���Ӻ� (rawMin={RawMin}s)", serviceName, Math.Round(nextDelay.TotalMinutes, 2), (int)accessExpire.TotalSeconds);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[multi:{Name}] ˢ���쳣��{Delay}s ������", serviceName, retryDelay.TotalSeconds);
                    nextDelay = retryDelay;
                    retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                }
            Delay:
                if (nextDelay <= TimeSpan.Zero) nextDelay = TimeSpan.FromMinutes(1);
                try { await Task.Delay(nextDelay, token); } catch { break; }
            }
            _logger.LogInformation("[multi:{Name}] ˢ��ѭ������", serviceName);
        }
    }
}
