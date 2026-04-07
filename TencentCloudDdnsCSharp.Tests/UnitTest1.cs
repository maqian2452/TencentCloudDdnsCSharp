using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using TencentCloudDdnsCSharp.Configuration;
using TencentCloudDdnsCSharp.DnsPod;
using TencentCloudDdnsCSharp.Ip;

namespace TencentCloudDdnsCSharp.Tests;

public class UnitTest1
{
    [Fact]
    public void TryLoadFromFile_LoadsValidConfig()
    {
        var file = CreateTempConfig("""
        {
          "IntervalMinutes": 15,
          "SecretId": "id",
          "SecretKey": "key",
          "Domain": "wor.fun",
          "SubDomain": "ipv6",
          "RecordType": "AAAA",
          "IpProviders": [
            { "Provider": "URL", "Url": "http://v6.ip.zxinc.org/getip" }
          ]
        }
        """);

        var loaded = DdnsConfig.TryLoadFromFile(file, out var config, out var error);

        Assert.True(loaded, error);
        Assert.NotNull(config);
        Assert.Equal("AAAA", config!.RecordType);
        Assert.Equal("AAAA&ipv6.wor.fun", config.Name);
    }

    [Fact]
    public void TryLoadFromFile_RejectsMissingCredentials()
    {
        var file = CreateTempConfig("""
        {
          "IntervalMinutes": 15,
          "Domain": "wor.fun",
          "SubDomain": "ipv6",
          "RecordType": "AAAA"
        }
        """);

        var loaded = DdnsConfig.TryLoadFromFile(file, out _, out var error);

        Assert.False(loaded);
        Assert.Contains("SecretId", error);
    }

    [Fact]
    public async Task UrlIpProvider_ExtractsIpv6()
    {
        var provider = new UrlIpProvider("http://example.test", new FakeHttpFetcher("2409:8a70:b72:12d0::1234"));

        var result = await provider.ResolveAsync(AddressFamily.InterNetworkV6, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("2409:8a70:b72:12d0::1234", IPAddress.Parse(result.Value!).ToString());
    }

    [Fact]
    public async Task LocalIpProvider_UsesResolvedAddress()
    {
        var provider = new LocalIpProvider("Ethernet", "2409:", new FakeLocalIpResolver(IPAddress.Parse("2409:8a70::1")));

        var result = await provider.ResolveAsync(AddressFamily.InterNetworkV6, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("2409:8a70::1", result.Value);
    }

    [Fact]
    public void IpProviderFactory_DefaultsToLocalForIpv6()
    {
        var factory = new IpProviderFactory(new FakeHttpFetcher("1.1.1.1"), new FakeLocalIpResolver(IPAddress.Parse("2409:8a70::2")));
        var config = new DdnsConfig
        {
            SecretId = "id",
            SecretKey = "key",
            Domain = "wor.fun",
            SubDomain = "ipv6",
            RecordType = "AAAA"
        };

        var providers = factory.CreateProviders(config);

        Assert.Single(providers);
        Assert.Contains("LOCAL", providers[0].Name);
    }

    [Fact]
    public void ModifyDynamicDnsRequest_SerializesTtlWithExpectedCase()
    {
        var requestType = typeof(TencentDnsPodClient).GetNestedType("ModifyDynamicDnsRequest", BindingFlags.NonPublic);
        Assert.NotNull(requestType);

        var request = Activator.CreateInstance(requestType!);
        Assert.NotNull(request);

        requestType!.GetProperty("Domain")!.SetValue(request, "wor.fun");
        requestType.GetProperty("SubDomain")!.SetValue(request, "ipv6");
        requestType.GetProperty("RecordId")!.SetValue(request, 1L);
        requestType.GetProperty("RecordLine")!.SetValue(request, "default");
        requestType.GetProperty("Value")!.SetValue(request, "2409:8a70::1");
        requestType.GetProperty("TTL")!.SetValue(request, 600);

        var json = JsonSerializer.Serialize(request, requestType);

        Assert.Contains("\"Ttl\":600", json);
        Assert.DoesNotContain("\"TTL\":600", json);
    }

    private static string CreateTempConfig(string content)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, "test.json");
        File.WriteAllText(file, content);
        return file;
    }

    private sealed class FakeHttpFetcher(string response) : IHttpResponseFetcher
    {
        public Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }

    private sealed class FakeLocalIpResolver(IPAddress? address) : ILocalIpResolver
    {
        public IPAddress? Resolve(AddressFamily addressFamily, string adapterName, string prefix)
        {
            return address;
        }
    }
}
