using System.Net.Sockets;

namespace TencentCloudDdnsCSharp.Ip;

internal sealed class LocalIpProvider(
    string adapterName,
    string prefix,
    ILocalIpResolver localIpResolver) : IIpProvider
{
    public string Name => $"LOCAL {adapterName} ({prefix}*)".Trim();

    public Task<IpResolutionResult> ResolveAsync(AddressFamily addressFamily, CancellationToken cancellationToken)
    {
        var address = localIpResolver.Resolve(addressFamily, adapterName, prefix);
        if (address is null)
        {
            return Task.FromResult(IpResolutionResult.Fail("no valid local address found"));
        }

        return Task.FromResult(IpResolutionResult.Ok(address.ToString()));
    }
}
