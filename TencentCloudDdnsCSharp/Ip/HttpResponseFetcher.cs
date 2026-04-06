namespace TencentCloudDdnsCSharp.Ip;

internal sealed class HttpResponseFetcher(HttpClient httpClient) : IHttpResponseFetcher
{
    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
