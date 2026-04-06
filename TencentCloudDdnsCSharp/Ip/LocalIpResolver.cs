using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TencentCloudDdnsCSharp.Ip;

internal sealed class LocalIpResolver : ILocalIpResolver
{
    public IPAddress? Resolve(AddressFamily addressFamily, string adapterName, string prefix)
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(adapterName) &&
                nic.Name.IndexOf(adapterName, StringComparison.OrdinalIgnoreCase) < 0 &&
                nic.Description.IndexOf(adapterName, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            foreach (var addressInfo in nic.GetIPProperties().UnicastAddresses)
            {
                var address = addressInfo.Address;
                if (address.AddressFamily != addressFamily)
                {
                    continue;
                }

                if (IPAddress.IsLoopback(address))
                {
                    continue;
                }

                if (addressFamily == AddressFamily.InterNetworkV6 &&
                    (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal))
                {
                    continue;
                }

                var value = address.ToString();
                if (!string.IsNullOrWhiteSpace(prefix) &&
                    !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return address;
            }
        }

        return null;
    }
}
