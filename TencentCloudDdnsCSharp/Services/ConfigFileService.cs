using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TencentCloudDdnsCSharp.Configuration;
using TencentCloudDdnsCSharp.DnsPod;
using TencentCloudDdnsCSharp.Ip;

namespace TencentCloudDdnsCSharp.Services;

internal sealed class ConfigFileService(
    IAppPaths appPaths,
    IDnsPodClientFactory dnsPodClientFactory,
    IIpProviderFactory ipProviderFactory,
    ILoggerFactory loggerFactory,
    ILogger<ConfigFileService> logger) : IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<string, ManagedWorker> workers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> reloadTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim mutationLock = new(1, 1);
    private readonly CancellationTokenSource shutdown = new();
    private FileSystemWatcher? watcher;

    internal int LoadedConfigCount => workers.Count;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var configDirectory = appPaths.ConfigDirectory;
        Directory.CreateDirectory(configDirectory);

        watcher = new FileSystemWatcher(configDirectory, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };
        watcher.Created += OnCreatedOrChanged;
        watcher.Changed += OnCreatedOrChanged;
        watcher.Deleted += OnDeleted;
        watcher.Renamed += OnRenamed;
        watcher.EnableRaisingEvents = true;

        var files = Directory.GetFiles(configDirectory, "*.json");
        foreach (var file in files)
        {
            await LoadOrReloadAsync(file, cancellationToken);
        }

        logger.LogInformation("Config file service started, loaded {Count} config(s)", workers.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        shutdown.Cancel();

        if (watcher is not null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnCreatedOrChanged;
            watcher.Changed -= OnCreatedOrChanged;
            watcher.Deleted -= OnDeleted;
            watcher.Renamed -= OnRenamed;
            watcher.Dispose();
            watcher = null;
        }

        foreach (var pair in reloadTokens)
        {
            pair.Value.Cancel();
            pair.Value.Dispose();
        }

        reloadTokens.Clear();

        await mutationLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var worker in workers.Values)
            {
                await worker.Worker.StopAsync();
            }

            workers.Clear();
        }
        finally
        {
            mutationLock.Release();
        }

        logger.LogInformation("Config file service stopped");
    }

    public void Dispose()
    {
        watcher?.Dispose();
        shutdown.Dispose();
        mutationLock.Dispose();
    }

    private void OnCreatedOrChanged(object sender, FileSystemEventArgs e)
    {
        QueueReload(e.FullPath);
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        _ = Task.Run(() => RemoveWorkerAsync(e.FullPath, shutdown.Token));
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        _ = Task.Run(() => RemoveWorkerAsync(e.OldFullPath, shutdown.Token));
        QueueReload(e.FullPath);
    }

    private void QueueReload(string file)
    {
        if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenSource = new CancellationTokenSource();
        CancellationTokenSource? replaced = null;
        _ = reloadTokens.AddOrUpdate(
            file,
            tokenSource,
            (_, current) =>
            {
                replaced = current;
                current.Cancel();
                return tokenSource;
            });

        if (replaced is not null)
        {
            replaced.Dispose();
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, shutdown.Token);
                await Task.Delay(TimeSpan.FromMilliseconds(500), linked.Token);
                await LoadOrReloadAsync(file, linked.Token);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (reloadTokens.TryGetValue(file, out var current) && ReferenceEquals(current, tokenSource))
                {
                    reloadTokens.TryRemove(file, out _);
                }

                tokenSource.Dispose();
            }
        });
    }

    private async Task LoadOrReloadAsync(string file, CancellationToken cancellationToken)
    {
        if (!await WaitForFileReadyAsync(file, cancellationToken))
        {
            return;
        }

        if (!DdnsConfig.TryLoadFromFile(file, out var config, out var error) || config is null)
        {
            logger.LogWarning("Load config failed for {File}: {Error}", file, error);
            return;
        }

        await mutationLock.WaitAsync(cancellationToken);
        try
        {
            var duplicate = workers.Values.FirstOrDefault(worker =>
                !string.Equals(worker.Config.SourceFile, file, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(worker.Config.Identity, config.Identity, StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null)
            {
                logger.LogWarning(
                    "Skip config {File}: duplicate record identity {Identity} already loaded from {OtherFile}",
                    file,
                    config.Identity,
                    duplicate.Config.SourceFile);
                return;
            }

            if (workers.TryRemove(file, out var current))
            {
                await current.Worker.StopAsync();
            }

            var worker = new RecordWorker(
                config,
                dnsPodClientFactory.Create(config.SecretId, config.SecretKey),
                ipProviderFactory.CreateProviders(config),
                loggerFactory.CreateLogger<RecordWorker>());
            worker.Start();
            workers[file] = new ManagedWorker(config, worker);
            logger.LogInformation("Config loaded: {File} => {Identity}", file, config.Identity);
        }
        finally
        {
            mutationLock.Release();
        }
    }

    private async Task RemoveWorkerAsync(string file, CancellationToken cancellationToken)
    {
        await mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (!workers.TryRemove(file, out var worker))
            {
                return;
            }

            await worker.Worker.StopAsync();
            logger.LogInformation("Config removed: {File}", file);
        }
        finally
        {
            mutationLock.Release();
        }
    }

    private static async Task<bool> WaitForFileReadyAsync(string file, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(file))
            {
                return false;
            }

            try
            {
                await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (stream.Length >= 0)
                {
                    return true;
                }
            }
            catch (IOException)
            {
            }

            await Task.Delay(100, cancellationToken);
        }

        return false;
    }

    private sealed record ManagedWorker(DdnsConfig Config, RecordWorker Worker);
}
