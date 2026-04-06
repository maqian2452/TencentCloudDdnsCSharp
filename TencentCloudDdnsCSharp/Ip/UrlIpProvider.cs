using System.Net.Sockets;

namespace TencentCloudDdnsCSharp.Ip;

internal sealed class UrlIpProvider(string url, IHttpResponseFetcher httpResponseFetcher) : IIpProvider
{
    public string Name => $"URL {url}";

    public async Task<IpResolutionResult> ResolveAsync(AddressFamily addressFamily, CancellationToken cancellationToken)
    {
        var response = await httpResponseFetcher.GetStringAsync(url, cancellationToken);
        if (IpTextParser.TryExtract(response, addressFamily, out var ip))
        {
            return IpResolutionResult.Ok(ip);
        }

        return IpResolutionResult.Fail($"no valid {(addressFamily == AddressFamily.InterNetworkV6 ? "IPv6" : "IPv4")} address found");
    }
}
