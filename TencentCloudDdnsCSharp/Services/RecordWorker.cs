using System.Net;
using Microsoft.Extensions.Logging;
using TencentCloudDdnsCSharp.Configuration;
using TencentCloudDdnsCSharp.DnsPod;
using TencentCloudDdnsCSharp.Ip;

namespace TencentCloudDdnsCSharp.Services;

internal sealed class RecordWorker
{
    private readonly DdnsConfig config;
    private readonly IDnsPodClient dnsPodClient;
    private readonly IReadOnlyList<IIpProvider> ipProviders;
    private readonly ILogger<RecordWorker> logger;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private Task? runTask;

    public RecordWorker(
        DdnsConfig config,
        IDnsPodClient dnsPodClient,
        IReadOnlyList<IIpProvider> ipProviders,
        ILogger<RecordWorker> logger)
    {
        this.config = config;
        this.dnsPodClient = dnsPodClient;
        this.ipProviders = ipProviders;
        this.logger = logger;
    }

    public string Name => config.Identity;

    public void Start()
    {
        runTask = Task.Run(() => RunAsync(cancellationTokenSource.Token));
    }

    internal Task RunOnceAsync(CancellationToken cancellationToken)
    {
        return ExecuteOnceAsync(cancellationToken);
    }

    public async Task StopAsync()
    {
        cancellationTokenSource.Cancel();
        if (runTask is null)
        {
            return;
        }

        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!config.Enabled)
        {
            logger.LogInformation("[{Name}] disabled, skip worker start", Name);
            return;
        }

        logger.LogInformation("[{Name}] worker started", Name);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteOnceAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[{Name}] work cycle failed", Name);
            }

            await Task.Delay(TimeSpan.FromMinutes(config.IntervalMinutes), cancellationToken);
        }

        logger.LogInformation("[{Name}] worker stopped", Name);
    }

    private async Task ExecuteOnceAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Name}] do work ...", Name);
        var currentIp = await ResolveCurrentIpAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(currentIp))
        {
            logger.LogInformation("[{Name}] fetch current ip failed, skip", Name);
            return;
        }

        var records = await dnsPodClient.DescribeRecordsAsync(
            config.Domain,
            config.SubDomain,
            config.RecordType,
            config.RecordLine,
            cancellationToken);

        if (config.RecordId.HasValue)
        {
            records = records.Where(record => record.RecordId == config.RecordId.Value).ToArray();
        }

        if (records.Count > 1)
        {
            logger.LogWarning(
                "[{Name}] multiple records matched configuration, record ids: {RecordIds}",
                Name,
                string.Join(",", records.Select(record => record.RecordId)));
            return;
        }

        if (records.Count == 0)
        {
            if (config.RecordId.HasValue)
            {
                logger.LogWarning(
                    "[{Name}] configured record id {RecordId} was not found, skip update",
                    Name,
                    config.RecordId.Value);
                return;
            }

            if (!config.CreateIfMissing)
            {
                logger.LogInformation("[{Name}] record not found and CreateIfMissing=false, skip", Name);
                return;
            }

            var recordId = await dnsPodClient.CreateRecordAsync(
                config.Domain,
                config.SubDomain,
                config.RecordType,
                config.RecordLine,
                currentIp,
                config.Ttl,
                cancellationToken);

            logger.LogInformation(
                "[{Name}] record created, recordId={RecordId}, value={Value}",
                Name,
                recordId,
                currentIp);
            return;
        }

        var record = records[0];
        if (IpEquals(record.Value, currentIp))
        {
            logger.LogInformation("[{Name}] ip not changed, skip", Name);
            return;
        }

        await dnsPodClient.ModifyDynamicDnsAsync(
            record.RecordId,
            config.Domain,
            config.SubDomain,
            string.IsNullOrWhiteSpace(record.Line) ? config.RecordLine : record.Line,
            currentIp,
            config.Ttl,
            cancellationToken);

        logger.LogInformation(
            "[{Name}] record updated, recordId={RecordId}, value={Value}",
            Name,
            record.RecordId,
            currentIp);
    }

    private async Task<string?> ResolveCurrentIpAsync(CancellationToken cancellationToken)
    {
        foreach (var provider in ipProviders)
        {
            try
            {
                var result = await provider.ResolveAsync(config.AddressFamily, cancellationToken);
                if (result.Success && !string.IsNullOrWhiteSpace(result.Value))
                {
                    logger.LogInformation(
                        "[{Name}] fetch real ip from {Provider} success, current ip is ({Ip})",
                        Name,
                        provider.Name,
                        result.Value);
                    return result.Value;
                }

                logger.LogInformation(
                    "[{Name}] fetch real ip from {Provider} failed: {Message}",
                    Name,
                    provider.Name,
                    result.Message);
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex, "[{Name}] fetch real ip from {Provider} failed", Name, provider.Name);
            }
        }

        return null;
    }

    private static bool IpEquals(string left, string right)
    {
        if (IPAddress.TryParse(left, out var leftIp) && IPAddress.TryParse(right, out var rightIp))
        {
            return leftIp.Equals(rightIp);
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
