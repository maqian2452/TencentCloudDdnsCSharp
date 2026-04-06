namespace TencentCloudDdnsCSharp.DnsPod;

internal sealed class DnsPodRecord
{
    public long RecordId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Line { get; init; } = string.Empty;

    public string LineId { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public int Ttl { get; init; }
}
