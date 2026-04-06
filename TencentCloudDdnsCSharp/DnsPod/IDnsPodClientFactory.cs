namespace TencentCloudDdnsCSharp.DnsPod;

internal interface IDnsPodClientFactory
{
    IDnsPodClient Create(string secretId, string secretKey);
}
