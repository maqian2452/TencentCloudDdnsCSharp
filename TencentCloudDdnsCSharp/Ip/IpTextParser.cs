using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace TencentCloudDdnsCSharp.Ip;

internal static partial class IpTextParser
{
    [GeneratedRegex(@"((?:(?:25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d))", RegexOptions.Compiled)]
    private static partial Regex Ipv4Regex();

    [GeneratedRegex(@"[0-9A-Fa-f:.%]+", RegexOptions.Compiled)]
    private static partial Regex Ipv6CandidateRegex();

    public static bool TryExtract(string text, AddressFamily addressFamily, out string ip)
    {
        var trimmed = text.Trim();
        if (IPAddress.TryParse(trimmed, out var parsed) && parsed.AddressFamily == addressFamily)
        {
            ip = parsed.ToString();
            return true;
        }

        if (addressFamily == AddressFamily.InterNetworkV6)
        {
            foreach (Match candidate in Ipv6CandidateRegex().Matches(trimmed))
            {
                if (!candidate.Value.Contains(':', StringComparison.Ordinal))
                {
                    continue;
                }

                if (IPAddress.TryParse(candidate.Value, out parsed) && parsed.AddressFamily == addressFamily)
                {
                    ip = parsed.ToString();
                    return true;
                }
            }
        }
        else
        {
            var match = Ipv4Regex().Match(trimmed);
            if (match.Success && IPAddress.TryParse(match.Value, out parsed) && parsed.AddressFamily == addressFamily)
            {
                ip = parsed.ToString();
                return true;
            }
        }

        ip = string.Empty;
        return false;
    }
}
