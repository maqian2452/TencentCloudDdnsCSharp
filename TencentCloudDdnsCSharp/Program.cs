using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using TencentCloudDdnsCSharp.DnsPod;
using TencentCloudDdnsCSharp.Ip;
using TencentCloudDdnsCSharp.Logging;
using TencentCloudDdnsCSharp.Services;
using TencentCloudDdnsCSharp.Windows;

namespace TencentCloudDdnsCSharp;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var mode = CliModeParser.Parse(args);
        if (mode is CliMode.Install or CliMode.Uninstall)
        {
            Log.Logger = CreateLoggerConfiguration().CreateLogger();
            try
            {
                if (mode == CliMode.Install)
                {
                    await WindowsServiceInstaller.InstallAsync(CancellationToken.None);
                }
                else
                {
                    await WindowsServiceInstaller.UninstallAsync(CancellationToken.None);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Service command failed");
                return 1;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        Log.Logger = CreateLoggerConfiguration().CreateLogger();

        try
        {
            await CreateHostBuilder(mode, FilterHostArgs(args)).Build().RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Service host terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(CliMode mode, string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddHttpClient<IHttpResponseFetcher, HttpResponseFetcher>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(AppConstants.UserAgent);
                });
                services.AddSingleton<IAppPaths, DefaultAppPaths>();
                services.AddSingleton<ILocalIpResolver, LocalIpResolver>();
                services.AddSingleton<IIpProviderFactory, IpProviderFactory>();
                services.AddSingleton<IDnsPodClientFactory, TencentDnsPodClientFactory>();
                services.AddHostedService<ConfigFileService>();
            });

        if (mode != CliMode.Console)
        {
            builder.UseWindowsService(options => options.ServiceName = AppConstants.ServiceName);
        }

        return builder;
    }

    private static LoggerConfiguration CreateLoggerConfiguration()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Sink(new DailyFolderFileSink(AppContext.BaseDirectory));
    }

    private static string[] FilterHostArgs(IEnumerable<string> args)
    {
        return args.Where(arg =>
                !arg.Equals("-c", StringComparison.OrdinalIgnoreCase) &&
                !arg.Equals("/c", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
