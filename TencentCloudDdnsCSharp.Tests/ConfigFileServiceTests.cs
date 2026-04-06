using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TencentCloudDdnsCSharp;
using TencentCloudDdnsCSharp.Configuration;
using TencentCloudDdnsCSharp.DnsPod;
using TencentCloudDdnsCSharp.Ip;
using TencentCloudDdnsCSharp.Services;

namespace TencentCloudDdnsCSharp.Tests;

public class ConfigFileServiceTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public ConfigFileServiceTests()
    {
        Directory.CreateDirectory(tempDirectory);
    }

    [Fact]
    public async Task StartAsync_LoadsExistingConfigFiles()
    {
        File.WriteAllText(Path.Combine(tempDirectory, "wor.fun.json"), ValidConfigJson);
        var service = CreateService();

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(1, service.LoadedConfigCount);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FileWatcher_HandlesCreateAndDelete()
    {
        var service = CreateService();
        await service.StartAsync(CancellationToken.None);

        var file = Path.Combine(tempDirectory, "dynamic.json");
        File.WriteAllText(file, ValidConfigJson);
        await WaitUntilAsync(() => service.LoadedConfigCount == 1);

        File.Delete(file);
        await WaitUntilAsync(() => service.LoadedConfigCount == 0);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_IgnoresInvalidConfig()
    {
        File.WriteAllText(Path.Combine(tempDirectory, "invalid.json"), """{ "Domain": "wor.fun" }""");
        var service = CreateService();

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(0, service.LoadedConfigCount);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_AllowsSameHostWithDifferentRecordLines()
    {
        File.WriteAllText(Path.Combine(tempDirectory, "line-default.json"), ValidConfigJson);
        File.WriteAllText(
            Path.Combine(tempDirectory, "line-telecom.json"),
            ValidConfigJson.Replace("\"RecordLine\": \"默认\"", "\"RecordLine\": \"电信\""));

        var service = CreateService();

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(2, service.LoadedConfigCount);
        await service.StopAsync(CancellationToken.None);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    private ConfigFileService CreateService()
    {
        return new ConfigFileService(
            new TestAppPaths(tempDirectory),
            new FakeDnsPodClientFactory(),
            new FakeIpProviderFactory(),
            NullLoggerFactory.Instance,
            NullLogger<ConfigFileService>.Instance);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < timeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Condition was not met in time.");
    }

    private const string ValidConfigJson = """
    {
      "IntervalMinutes": 15,
      "SecretId": "id",
      "SecretKey": "key",
      "Domain": "wor.fun",
      "SubDomain": "ipv6",
      "RecordType": "AAAA",
      "RecordLine": "默认",
      "CreateIfMissing": true,
      "IpProviders": [
        { "Provider": "URL", "Url": "http://v6.ip.zxinc.org/getip" }
      ]
    }
    """;

    private sealed class TestAppPaths(string configDirectory) : IAppPaths
    {
        public string BaseDirectory => configDirectory;
        public string ConfigDirectory => configDirectory;
    }

    private sealed class FakeDnsPodClientFactory : IDnsPodClientFactory
    {
        public IDnsPodClient Create(string secretId, string secretKey)
        {
            return new FakeDnsPodClient();
        }
    }

    private sealed class FakeDnsPodClient : IDnsPodClient
    {
        public Task<IReadOnlyList<DnsPodRecord>> DescribeRecordsAsync(string domain, string subDomain, string recordType, string recordLine, CancellationToken cancellationToken)
        {
            IReadOnlyList<DnsPodRecord> records =
            [
                new DnsPodRecord
                {
                    RecordId = 1,
                    Name = "ipv6",
                    Type = "AAAA",
                    Line = "默认",
                    Value = "2409:8a70::1"
                }
            ];
            return Task.FromResult(records);
        }

        public Task<long> CreateRecordAsync(string domain, string subDomain, string recordType, string recordLine, string value, int ttl, CancellationToken cancellationToken)
        {
            return Task.FromResult(1L);
        }

        public Task ModifyDynamicDnsAsync(long recordId, string domain, string subDomain, string recordLine, string value, int ttl, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeIpProviderFactory : IIpProviderFactory
    {
        public IReadOnlyList<IIpProvider> CreateProviders(DdnsConfig config)
        {
            return [new FakeIpProvider()];
        }
    }

    private sealed class FakeIpProvider : IIpProvider
    {
        public string Name => "FAKE";

        public Task<IpResolutionResult> ResolveAsync(System.Net.Sockets.AddressFamily addressFamily, CancellationToken cancellationToken)
        {
            return Task.FromResult(IpResolutionResult.Ok("2409:8a70::1"));
        }
    }
}
