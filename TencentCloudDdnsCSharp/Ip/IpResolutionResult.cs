namespace TencentCloudDdnsCSharp.Ip;

internal sealed record IpResolutionResult(bool Success, string? Value, string Message)
{
    public static IpResolutionResult Ok(string value) => new(true, value, string.Empty);

    public static IpResolutionResult Fail(string message) => new(false, null, message);
}
