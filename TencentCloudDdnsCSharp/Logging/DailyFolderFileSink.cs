using Serilog.Core;
using Serilog.Events;

namespace TencentCloudDdnsCSharp.Logging;

internal sealed class DailyFolderFileSink(string baseDirectory) : ILogEventSink, IDisposable
{
    private readonly object syncRoot = new();
    private StreamWriter? writer;
    private string currentFilePath = string.Empty;

    public void Emit(LogEvent logEvent)
    {
        var filePath = Path.Combine(
            baseDirectory,
            "Logs",
            logEvent.Timestamp.LocalDateTime.ToString("yyyy-MM-dd"),
            "INFO.LOG");

        var line = $"{logEvent.Timestamp.LocalDateTime:yyyy-MM-dd HH:mm:ss.fff}|{logEvent.Level}: {logEvent.RenderMessage()}";
        if (logEvent.Exception is not null)
        {
            line = $"{line}{Environment.NewLine}{logEvent.Exception}";
        }

        lock (syncRoot)
        {
            if (!string.Equals(currentFilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                writer?.Dispose();
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                writer = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    AutoFlush = true
                };
                currentFilePath = filePath;
            }

            writer!.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            writer?.Dispose();
            writer = null;
            currentFilePath = string.Empty;
        }
    }
}
