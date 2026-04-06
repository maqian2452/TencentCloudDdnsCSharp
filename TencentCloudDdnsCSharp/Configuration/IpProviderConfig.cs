namespace TencentCloudDdnsCSharp.Configuration;

internal sealed class IpProviderConfig
{
    public string Provider { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string AdapterName { get; set; } = string.Empty;

    public string Prefix { get; set; } = string.Empty;

    public void Normalize()
    {
        Provider = Provider.Trim().ToUpperInvariant();
        Url = Url.Trim();
        AdapterName = AdapterName.Trim();
        Prefix = Prefix.Trim();
    }

    public bool TryValidate(out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(Provider))
        {
            error = "Provider is required.";
            return false;
        }

        if (Provider == "URL")
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out _))
            {
                error = "Url must be an absolute URI.";
                return false;
            }

            return true;
        }

        if (Provider == "LOCAL")
        {
            return true;
        }

        error = "Provider must be URL or LOCAL.";
        return false;
    }
}
