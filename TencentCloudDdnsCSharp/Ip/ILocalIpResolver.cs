using System.Net;
using System.Net.Sockets;

namespace TencentCloudDdnsCSharp.Ip;

internal interface ILocalIpResolver
{
    IPAddress? Resolve(AddressFamily addressFamily, string adapterName, string prefix);
}
