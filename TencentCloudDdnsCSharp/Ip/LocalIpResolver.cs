using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TencentCloudDdnsCSharp.Ip;

internal sealed class LocalIpResolver : ILocalIpResolver
{
    public IPAddress? Resolve(AddressFamily addressFamily, string adapterName, string prefix)
    {
        var candidates = new List<LocalAddressCandidate>();
        var hasAdapterFilter = !string.IsNullOrWhiteSpace(adapterName);
        var hasPrefixFilter = !string.IsNullOrWhiteSpace(prefix);

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

            if (hasAdapterFilter &&
                nic.Name.IndexOf(adapterName, StringComparison.OrdinalIgnoreCase) < 0 &&
                nic.Description.IndexOf(adapterName, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            foreach (var addressInfo in nic.GetIPProperties().UnicastAddresses)
            {
                var address = addressInfo.Address;
                var value = address.ToString();
                var prefixMatched = !hasPrefixFilter || value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                if (!IsUsableAddress(addressInfo, addressFamily, prefixMatched))
                {
                    continue;
                }

                candidates.Add(new LocalAddressCandidate(
                    address,
                    GetInterfaceScore(nic, hasAdapterFilter) + GetAddressScore(addressInfo, addressFamily, prefixMatched)));
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Address.ToString(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()
            ?.Address;
    }

    private static bool IsUniqueLocalIpv6(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes.Length == 16 && (bytes[0] & 0xFE) == 0xFC;
    }

    private static bool IsUsableAddress(
        UnicastIPAddressInformation addressInfo,
        AddressFamily addressFamily,
        bool prefixMatched)
    {
        var address = addressInfo.Address;
        if (address.AddressFamily != addressFamily)
        {
            return false;
        }

        if (!prefixMatched || IPAddress.IsLoopback(address))
        {
            return false;
        }

        if (TryGetDuplicateAddressDetectionState(addressInfo) is
            DuplicateAddressDetectionState.Deprecated or
            DuplicateAddressDetectionState.Duplicate or
            DuplicateAddressDetectionState.Invalid or
            DuplicateAddressDetectionState.Tentative)
        {
            return false;
        }

        return addressFamily != AddressFamily.InterNetworkV6 ||
            (!address.IsIPv6LinkLocal &&
             !address.IsIPv6Multicast &&
             !address.IsIPv6SiteLocal &&
             !IsUniqueLocalIpv6(address));
    }

    private static int GetInterfaceScore(NetworkInterface nic, bool hasAdapterFilter)
    {
        var score = hasAdapterFilter ? 10_000 : 0;
        score += nic.NetworkInterfaceType switch
        {
            NetworkInterfaceType.Wireless80211 => 1_000,
            NetworkInterfaceType.Ethernet => 950,
            NetworkInterfaceType.GigabitEthernet => 950,
            NetworkInterfaceType.Tunnel => -1_000,
            NetworkInterfaceType.Ppp => -500,
            _ => 0
        };

        if (LooksVirtualOrTunnel(nic.Name) || LooksVirtualOrTunnel(nic.Description))
        {
            score -= hasAdapterFilter ? 100 : 500;
        }

        return score;
    }

    private static int GetAddressScore(UnicastIPAddressInformation addressInfo, AddressFamily addressFamily, bool prefixMatched)
    {
        var metadata = GetAddressMetadata(addressInfo);

        return GetAddressScore(
            addressFamily,
            metadata.PrefixOrigin,
            metadata.SuffixOrigin,
            metadata.DuplicateAddressDetectionState,
            metadata.IsTransient,
            metadata.IsDnsEligible,
            prefixMatched);
    }

    private static int GetAddressScore(
        AddressFamily addressFamily,
        PrefixOrigin? prefixOrigin,
        SuffixOrigin? suffixOrigin,
        DuplicateAddressDetectionState? duplicateAddressDetectionState,
        bool isTransient,
        bool isDnsEligible,
        bool prefixMatched)
    {
        var score = prefixMatched ? 500 : 0;
        score += duplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred ? 500 : 0;
        score += addressFamily == AddressFamily.InterNetworkV6 ? 500 : 0;
        score += isDnsEligible ? 100 : -100;
        score += isTransient ? -100 : 0;

        score += prefixOrigin switch
        {
            PrefixOrigin.RouterAdvertisement => 300,
            PrefixOrigin.Dhcp => 250,
            PrefixOrigin.Manual => 0,
            PrefixOrigin.WellKnown => -300,
            _ => 0
        };

        score += suffixOrigin switch
        {
            SuffixOrigin.LinkLayerAddress => 400,
            SuffixOrigin.OriginDhcp => 300,
            SuffixOrigin.Random => 100,
            SuffixOrigin.Manual => 0,
            _ => 0
        };

        return score;
    }

    private static AddressMetadata GetAddressMetadata(UnicastIPAddressInformation addressInfo)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new AddressMetadata(null, null, null, false, true);
        }

        return new AddressMetadata(
            addressInfo.PrefixOrigin,
            addressInfo.SuffixOrigin,
            addressInfo.DuplicateAddressDetectionState,
            addressInfo.IsTransient,
            addressInfo.IsDnsEligible);
    }

    private static DuplicateAddressDetectionState? TryGetDuplicateAddressDetectionState(UnicastIPAddressInformation addressInfo)
    {
        return OperatingSystem.IsWindows()
            ? addressInfo.DuplicateAddressDetectionState
            : null;
    }

    private static bool LooksVirtualOrTunnel(string value)
    {
        return value.Contains("virtual", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("hyper-v", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("wsl", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("tunnel", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("tap", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("vpn", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("clash", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("meta", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record LocalAddressCandidate(IPAddress Address, int Score);

    private sealed record AddressMetadata(
        PrefixOrigin? PrefixOrigin,
        SuffixOrigin? SuffixOrigin,
        DuplicateAddressDetectionState? DuplicateAddressDetectionState,
        bool IsTransient,
        bool IsDnsEligible);
}
