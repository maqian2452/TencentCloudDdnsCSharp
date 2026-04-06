using System.Diagnostics;
using Serilog;

namespace TencentCloudDdnsCSharp.Windows;

internal static class WindowsServiceInstaller
{
    public static async Task InstallAsync(CancellationToken cancellationToken)
    {
        var exePath = Environment.ProcessPath ?? throw new InvalidOperationException("Unable to resolve executable path.");
        if (await ServiceExistsAsync(cancellationToken))
        {
            await RunScAsync(
                $"config {AppConstants.ServiceName} binPath= {QuoteForSc(exePath)} start= auto DisplayName= {QuoteForSc(AppConstants.ServiceDisplayName)}",
                cancellationToken);
        }
        else
        {
            await RunScAsync(
                $"create {AppConstants.ServiceName} binPath= {QuoteForSc(exePath)} start= auto DisplayName= {QuoteForSc(AppConstants.ServiceDisplayName)}",
                cancellationToken);
        }

        await RunScAsync(
            $"description {AppConstants.ServiceName} {QuoteForSc(AppConstants.ServiceDescription)}",
            cancellationToken);

        Log.Information("Service {ServiceName} installed or updated", AppConstants.ServiceName);
    }

    public static async Task UninstallAsync(CancellationToken cancellationToken)
    {
        await RunScAsync($"stop {AppConstants.ServiceName}", cancellationToken, allowFailure: true);
        await RunScAsync($"delete {AppConstants.ServiceName}", cancellationToken, allowFailure: true);
        Log.Information("Service {ServiceName} uninstalled", AppConstants.ServiceName);
    }

    private static async Task<bool> ServiceExistsAsync(CancellationToken cancellationToken)
    {
        var result = await RunScAsync($"query {AppConstants.ServiceName}", cancellationToken, allowFailure: true);
        return result.ExitCode == 0;
    }

    private static async Task<ProcessResult> RunScAsync(string arguments, CancellationToken cancellationToken, bool allowFailure = false)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0 && !allowFailure)
        {
            throw new InvalidOperationException(
                $"sc.exe {arguments} failed with code {process.ExitCode}: {stdout} {stderr}".Trim());
        }

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            Log.Information("sc.exe {Arguments}: {Output}", arguments, stdout.Trim());
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Log.Warning("sc.exe {Arguments}: {Error}", arguments, stderr.Trim());
        }

        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    private static string QuoteForSc(string value)
    {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
}
