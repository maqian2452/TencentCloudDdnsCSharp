using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TencentCloudDdnsCSharp.Configuration;

internal sealed class DdnsConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    [JsonIgnore]
    public string SourceFile { get; private set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 15;

    public string SecretId { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string SubDomain { get; set; } = "@";

    public string RecordType { get; set; } = "A";

    public string RecordLine { get; set; } = "默认";

    public int Ttl { get; set; } = 600;

    public long? RecordId { get; set; }

    public bool CreateIfMissing { get; set; } = true;

    public List<IpProviderConfig> IpProviders { get; set; } = [];

    [JsonIgnore]
    public string Name => $"{RecordType}&{SubDomain}.{Domain}";

    [JsonIgnore]
    public string Identity => RecordId.HasValue
        ? $"{Name}#{RecordId.Value}"
        : $"{Name}@{RecordLine}";

    [JsonIgnore]
    public AddressFamily AddressFamily => IsIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

    [JsonIgnore]
    public bool IsIpv6 => string.Equals(RecordType, "AAAA", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsIpv4 => string.Equals(RecordType, "A", StringComparison.OrdinalIgnoreCase);

    public static bool TryLoadFromFile(string file, out DdnsConfig? config, out string error)
    {
        config = null;
        error = string.Empty;

        try
        {
            using var stream = File.OpenRead(file);
            var parsed = JsonSerializer.Deserialize<DdnsConfig>(stream, JsonOptions);
            if (parsed is null)
            {
                error = "Configuration file is empty.";
                return false;
            }

            parsed.Normalize(file);
            var validationErrors = parsed.Validate();
            if (validationErrors.Count > 0)
            {
                error = string.Join("; ", validationErrors);
                return false;
            }

            config = parsed;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (IntervalMinutes <= 0)
        {
            errors.Add("IntervalMinutes must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(SecretId))
        {
            errors.Add("SecretId is required.");
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            errors.Add("SecretKey is required.");
        }

        if (string.IsNullOrWhiteSpace(Domain) || !Domain.Contains('.'))
        {
            errors.Add("Domain must be a valid root domain.");
        }

        if (string.IsNullOrWhiteSpace(SubDomain))
        {
            errors.Add("SubDomain is required.");
        }

        if (!IsIpv4 && !IsIpv6)
        {
            errors.Add("RecordType must be A or AAAA.");
        }

        if (Ttl <= 0)
        {
            errors.Add("Ttl must be greater than 0.");
        }

        if (RecordId.HasValue && RecordId.Value <= 0)
        {
            errors.Add("RecordId must be greater than 0.");
        }

        for (var i = 0; i < IpProviders.Count; i++)
        {
            if (!IpProviders[i].TryValidate(out var providerError))
            {
                errors.Add($"IpProviders[{i}] {providerError}");
            }
        }

        return errors;
    }

    private void Normalize(string file)
    {
        SourceFile = file;
        Domain = Domain.Trim();
        SubDomain = string.IsNullOrWhiteSpace(SubDomain) ? "@" : SubDomain.Trim();
        RecordType = RecordType.Trim().ToUpperInvariant();
        RecordLine = string.IsNullOrWhiteSpace(RecordLine) ? "默认" : RecordLine.Trim();
        IpProviders ??= [];

        foreach (var provider in IpProviders)
        {
            provider.Normalize();
        }
    }
}
