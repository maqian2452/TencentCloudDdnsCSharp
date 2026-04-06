namespace TencentCloudDdnsCSharp.DnsPod;

internal sealed class TencentDnsPodClientFactory : IDnsPodClientFactory
{
    public IDnsPodClient Create(string secretId, string secretKey)
    {
        return new TencentDnsPodClient(secretId, secretKey);
    }
}
