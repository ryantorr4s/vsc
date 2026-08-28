using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace LogParser;

public sealed class LogEntry
{
    public DateTime Timestamp { get; }
    public string Level { get; }
    public string Thread { get; }
    public string Logger { get; }
    public string Message { get; }

    public LogEntry(
        DateTime timestamp,
        string level,
        string thread,
        string logger,
        string message)
    {
        Timestamp = timestamp;
        Level = level;
        Thread = thread;
        Logger = logger;
        Message = message;
    }

    public override string ToString() =>
        $"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{Thread}] {Logger} - {Message}";
}

public sealed class TextLogParser
{
    // Expected format:
    // 2026-08-17 11:08:44 [INFO] [Thread-1] MyNamespace.MyClass - User logged in successfully.
    private static readonly Regex LogRegex = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\s+" +
        @"\[(?<level>[^\]]+)\]\s+" +
        @"\[(?<thread>[^\]]+)\]\s+" +
        @"(?<logger>[\w.]+)\s+-\s+" +
        @"(?<message>.*)$"
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

    public IEnumerable<LogEntry> ParseLogFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("O caminho do arquivo não pode ser vazio.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("O arquivo de log não foi encontrado.", filePath);

        using var reader = new StreamReader(filePath);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (TryParseLine(line, out var entry))
                yield return entry!;
        }
    }

    public bool TryParseLine(string line, out LogEntry? entry)
    {
        entry = null;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        var match = LogRegex.Match(line);

        if (!match.Success)
            return false;

        if (!DateTime.TryParseExact(
                match.Groups["timestamp"].Value,
                TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp))
        {
            return false;
        }

        entry = new LogEntry(
            timestamp: timestamp,
            level: match.Groups["level"].Value,
            thread: match.Groups["thread"].Value,
            logger: match.Groups["logger"].Value,
            message: match.Groups["message"].Value);

        return true;
    }
}