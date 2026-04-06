using TencentCloudDdnsCSharp.Configuration;

namespace TencentCloudDdnsCSharp.Ip;

internal interface IIpProviderFactory
{
    IReadOnlyList<IIpProvider> CreateProviders(DdnsConfig config);
}

internal sealed class IpProviderFactory(
    IHttpResponseFetcher httpResponseFetcher,
    ILocalIpResolver localIpResolver) : IIpProviderFactory
{
    public IReadOnlyList<IIpProvider> CreateProviders(DdnsConfig config)
    {
        var providers = new List<IpProviderConfig>(config.IpProviders);
        if (providers.Count == 0)
        {
            providers.Add(config.IsIpv6
                ? new IpProviderConfig { Provider = "LOCAL" }
                : new IpProviderConfig { Provider = "URL", Url = AppConstants.DefaultIpv4Url });
        }

        return providers.Select(provider => CreateProvider(provider)).ToArray();
    }

    private IIpProvider CreateProvider(IpProviderConfig config)
    {
        return config.Provider switch
        {
            "URL" => new UrlIpProvider(config.Url, httpResponseFetcher),
            "LOCAL" => new LocalIpProvider(config.AdapterName, config.Prefix, localIpResolver),
            _ => throw new InvalidOperationException($"Unsupported provider {config.Provider}.")
        };
    }
}
