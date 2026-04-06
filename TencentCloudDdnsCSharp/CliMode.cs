namespace TencentCloudDdnsCSharp;

internal enum CliMode
{
    Run,
    Console,
    Install,
    Uninstall
}

internal static class CliModeParser
{
    public static CliMode Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return CliMode.Run;
        }

        return args[0].ToLowerInvariant() switch
        {
            "-c" or "/c" => CliMode.Console,
            "-i" or "/i" => CliMode.Install,
            "-u" or "/u" => CliMode.Uninstall,
            _ => CliMode.Run
        };
    }
}
