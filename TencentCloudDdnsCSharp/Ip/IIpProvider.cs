using System.Net.Sockets;

namespace TencentCloudDdnsCSharp.Ip;

internal interface IIpProvider
{
    string Name { get; }

    Task<IpResolutionResult> ResolveAsync(AddressFamily addressFamily, CancellationToken cancellationToken);
}
