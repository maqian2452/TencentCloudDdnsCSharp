namespace TencentCloudDdnsCSharp;

internal interface IAppPaths
{
    string BaseDirectory { get; }

    string ConfigDirectory { get; }
}

internal sealed class DefaultAppPaths : IAppPaths
{
    public string BaseDirectory => AppContext.BaseDirectory;

    public string ConfigDirectory => Path.Combine(BaseDirectory, AppConstants.ConfigDirectoryName);
}
