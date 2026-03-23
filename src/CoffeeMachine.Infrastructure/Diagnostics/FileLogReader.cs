using CoffeeMachine.Application;
using Microsoft.Extensions.Options;
using System.Text;

namespace CoffeeMachine.Infrastructure.Diagnostics;

public sealed class LogFileOptions
{
    public string Path { get; set; } = "logs/coffee-machine-.log";
}

public sealed class FileLogReader(IOptions<LogFileOptions> options) : ILogReader
{
    public async Task<IReadOnlyCollection<string>> ReadRecentAsync(int lines, CancellationToken cancellationToken = default)
    {
        var directory = global::System.IO.Path.GetDirectoryName(options.Value.Path) ?? "logs";
        if (!Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        var latestFile = new DirectoryInfo(directory)
            .GetFiles("*.log")
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latestFile is null)
        {
            return Array.Empty<string>();
        }

        try
        {
            await using var stream = new FileStream(
                latestFile.FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync(cancellationToken);
            var recent = content
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(Math.Max(1, lines))
                .ToArray();

            return recent;
        }
        catch (IOException)
        {
            // Logging should never break the diagnostics endpoint. If the file is temporarily locked
            // or rotated, return an empty result and let the next polling cycle retry.
            return Array.Empty<string>();
        }
    }
}
