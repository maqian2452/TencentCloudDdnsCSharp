namespace TencentCloudDdnsCSharp.DnsPod;

internal interface IDnsPodClient
{
    Task<IReadOnlyList<DnsPodRecord>> DescribeRecordsAsync(
        string domain,
        string subDomain,
        string recordType,
        string recordLine,
        CancellationToken cancellationToken);

    Task<long> CreateRecordAsync(
        string domain,
        string subDomain,
        string recordType,
        string recordLine,
        string value,
        int ttl,
        CancellationToken cancellationToken);

    Task ModifyDynamicDnsAsync(
        long recordId,
        string domain,
        string subDomain,
        string recordLine,
        string value,
        int ttl,
        CancellationToken cancellationToken);
}
