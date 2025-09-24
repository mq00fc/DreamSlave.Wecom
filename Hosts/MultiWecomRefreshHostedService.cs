using System.Collections.Concurrent;

namespace DreamSlave.Wecom.Hosts
{
    /// <summary>
    /// ͳһ��������ҵ΢��ʵ��ˢ�����񣨻��� keyed ����̬������
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
            // �� ServiceCollectionExtensions �ľ�̬ע����ȡʵ����Ϣ
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
                _logger.LogWarning(ex, "[multi:{Name}] δ�ҵ���Ӧ�� OAuth2 �����˳�ˢ��ѭ��", name);
                return;
            }

            var config = oauth.GetConfig();
            var safety = TimeSpan.FromMinutes(5);
            var retryDelay = TimeSpan.FromSeconds(10);
            var maxRetryDelay = TimeSpan.FromMinutes(5);
            var maxInterval = TimeSpan.FromHours(1);
            _logger.LogInformation("[multi:{Name}] ˢ��ѭ������ (CorpID={CorpID}, AgentId={AgentId})", name, config.CorpID, config.AgentId);

            // ����Ԥ��
            try
            {
                var t = await oauth.RefreshAccessTokenAsync();
                if (t == null)
                    _logger.LogWarning("[multi:{Name}] Ԥ�� AccessToken ʧ��", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[multi:{Name}] Ԥ���쳣", name);
            }

            while (!token.IsCancellationRequested)
            {
                TimeSpan nextDelay;
                try
                {
                    var accessToken = await oauth.RefreshAccessTokenAsync();
                    if (accessToken == null || accessToken.Errcode != 0 || string.IsNullOrEmpty(accessToken.AccessToken))
                    {
                        _logger.LogWarning("[multi:{Name}] AccessToken ˢ��ʧ�ܣ�{Delay}s ������", name, retryDelay.TotalSeconds);
                        nextDelay = retryDelay;
                        retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                        goto Delay;
                    }

                    var accessExpire = TimeSpan.FromSeconds(Math.Max(accessToken.ExpiresIn, 60));
                    nextDelay = accessExpire > safety ? accessExpire - safety : TimeSpan.FromMinutes(1);
                    if (nextDelay > maxInterval) nextDelay = maxInterval;
                    retryDelay = TimeSpan.FromSeconds(10);
                    _logger.LogInformation("[multi:{Name}] ˢ�³ɹ����´� {Minutes} ���Ӻ� (rawMin={RawMin}s)", name, Math.Round(nextDelay.TotalMinutes, 2), (int)accessExpire.TotalSeconds);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[multi:{Name}] ˢ���쳣��{Delay}s ������", name, retryDelay.TotalSeconds);
                    nextDelay = retryDelay;
                    retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 2, maxRetryDelay.TotalMilliseconds));
                }
            Delay:
                if (nextDelay <= TimeSpan.Zero) nextDelay = TimeSpan.FromMinutes(1);
                try { await Task.Delay(nextDelay, token); } catch { break; }
            }
            _logger.LogInformation("[multi:{Name}] ˢ��ѭ������", name);
        }
    }
}
