namespace TencentCloudDdnsCSharp.Ip;

internal interface IHttpResponseFetcher
{
    Task<string> GetStringAsync(string url, CancellationToken cancellationToken);
}
