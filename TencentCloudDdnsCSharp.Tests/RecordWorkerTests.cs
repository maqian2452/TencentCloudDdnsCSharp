using Microsoft.Extensions.Logging.Abstractions;
using TencentCloudDdnsCSharp.Configuration;
using TencentCloudDdnsCSharp.DnsPod;
using TencentCloudDdnsCSharp.Ip;
using TencentCloudDdnsCSharp.Services;

namespace TencentCloudDdnsCSharp.Tests;

public class RecordWorkerTests
{
    [Fact]
    public async Task RunOnceAsync_SkipsWhenIpUnchanged()
    {
        var client = new FakeDnsPodClient
        {
            Records =
            [
                new DnsPodRecord
                {
                    RecordId = 1,
                    Name = "ipv6",
                    Type = "AAAA",
                    Line = "默认",
                    Value = "2409:8a70::1"
                }
            ]
        };
        var worker = CreateWorker(client, [new FakeIpProvider(IpResolutionResult.Ok("2409:8a70::1"))]);

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(0, client.ModifiedCount);
        Assert.Equal(0, client.CreatedCount);
    }

    [Fact]
    public async Task RunOnceAsync_UpdatesExistingRecord()
    {
        var client = new FakeDnsPodClient
        {
            Records =
            [
                new DnsPodRecord
                {
                    RecordId = 2,
                    Name = "ipv6",
                    Type = "AAAA",
                    Line = "默认",
                    Value = "2409:8a70::1"
                }
            ]
        };
        var worker = CreateWorker(client, [new FakeIpProvider(IpResolutionResult.Ok("2409:8a70::2"))]);

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, client.ModifiedCount);
        Assert.Equal(2L, client.LastModifiedRecordId);
        Assert.Equal("2409:8a70::2", client.LastModifiedValue);
    }

    [Fact]
    public async Task RunOnceAsync_CreatesRecordWhenMissing()
    {
        var client = new FakeDnsPodClient();
        var worker = CreateWorker(client, [new FakeIpProvider(IpResolutionResult.Ok("2409:8a70::3"))]);

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, client.CreatedCount);
        Assert.Equal("2409:8a70::3", client.LastCreatedValue);
    }

    [Fact]
    public async Task RunOnceAsync_FallsBackToSecondProvider()
    {
        var client = new FakeDnsPodClient();
        var worker = CreateWorker(
            client,
            [
                new FakeIpProvider(IpResolutionResult.Fail("first failed")),
                new FakeIpProvider(IpResolutionResult.Ok("2409:8a70::4"))
            ]);

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, client.CreatedCount);
        Assert.Equal("2409:8a70::4", client.LastCreatedValue);
    }

    [Fact]
    public async Task RunOnceAsync_UsesConfiguredRecordId()
    {
        var client = new FakeDnsPodClient
        {
            Records =
            [
                new DnsPodRecord { RecordId = 10, Name = "ipv6", Type = "AAAA", Line = "默认", Value = "2409:8a70::10" },
                new DnsPodRecord { RecordId = 11, Name = "ipv6", Type = "AAAA", Line = "默认", Value = "2409:8a70::11" }
            ]
        };

        var config = CreateConfig();
        config.RecordId = 11;
        var worker = new RecordWorker(
            config,
            client,
            [new FakeIpProvider(IpResolutionResult.Ok("2409:8a70::12"))],
            NullLogger<RecordWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, client.ModifiedCount);
        Assert.Equal(11L, client.LastModifiedRecordId);
    }

    [Fact]
    public async Task RunOnceAsync_DoesNotCreateWhenConfiguredRecordIdIsMissing()
    {
        var client = new FakeDnsPodClient();
        var config = CreateConfig();
        config.RecordId = 11;

        var worker = new RecordWorker(
            config,
            client,
            [new FakeIpProvider(IpResolutionResult.Ok("2409:8a70::12"))],
            NullLogger<RecordWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(0, client.CreatedCount);
        Assert.Equal(0, client.ModifiedCount);
    }

    private static RecordWorker CreateWorker(FakeDnsPodClient client, IReadOnlyList<IIpProvider> providers)
    {
        return new RecordWorker(CreateConfig(), client, providers, NullLogger<RecordWorker>.Instance);
    }

    private static DdnsConfig CreateConfig()
    {
        return new DdnsConfig
        {
            SecretId = "id",
            SecretKey = "key",
            Domain = "wor.fun",
            SubDomain = "ipv6",
            RecordType = "AAAA",
            RecordLine = "默认",
            Ttl = 600,
            CreateIfMissing = true
        };
    }

    private sealed class FakeDnsPodClient : IDnsPodClient
    {
        public IReadOnlyList<DnsPodRecord> Records { get; set; } = [];
        public int CreatedCount { get; private set; }
        public int ModifiedCount { get; private set; }
        public long LastModifiedRecordId { get; private set; }
        public string LastModifiedValue { get; private set; } = string.Empty;
        public string LastCreatedValue { get; private set; } = string.Empty;

        public Task<IReadOnlyList<DnsPodRecord>> DescribeRecordsAsync(string domain, string subDomain, string recordType, string recordLine, CancellationToken cancellationToken)
        {
            return Task.FromResult(Records);
        }

        public Task<long> CreateRecordAsync(string domain, string subDomain, string recordType, string recordLine, string value, int ttl, CancellationToken cancellationToken)
        {
            CreatedCount++;
            LastCreatedValue = value;
            return Task.FromResult(99L);
        }

        public Task ModifyDynamicDnsAsync(long recordId, string domain, string subDomain, string recordLine, string value, int ttl, CancellationToken cancellationToken)
        {
            ModifiedCount++;
            LastModifiedRecordId = recordId;
            LastModifiedValue = value;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeIpProvider(IpResolutionResult result) : IIpProvider
    {
        public string Name => "FAKE";

        public Task<IpResolutionResult> ResolveAsync(System.Net.Sockets.AddressFamily addressFamily, CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }
}
