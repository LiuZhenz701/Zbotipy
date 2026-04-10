using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalMusicPlayer.Services;

public sealed class LyricsService
{
    private static readonly Regex TimestampLineRegex = new(
        @"^\[(\d+):(\d{2})(?:\.(\d{1,3}))?\]\s*(.*)$",
        RegexOptions.Compiled);

    private readonly List<(TimeSpan Time, string Text)> _lines = new();

    public void LoadForAudioFile(string audioFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioFilePath);

        _lines.Clear();

        var lrcPath = Path.ChangeExtension(audioFilePath, ".lrc");
        if (!File.Exists(lrcPath))
            return;

        foreach (var raw in File.ReadAllLines(lrcPath, Encoding.UTF8))
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
                continue;

            var match = TimestampLineRegex.Match(trimmed);
            if (!match.Success)
                continue;

            var minutes = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var seconds = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var fractionalSeconds = match.Groups[3].Success
                ? ParseFractionalSeconds(match.Groups[3].Value)
                : 0d;

            var text = match.Groups[4].Value.Trim();
            if (text.Length == 0)
                continue;

            var time = TimeSpan.FromMinutes(minutes)
                + TimeSpan.FromSeconds(seconds)
                + TimeSpan.FromSeconds(fractionalSeconds);

            if (time < TimeSpan.Zero)
                continue;

            _lines.Add((time, text));
        }

        _lines.Sort((a, b) => a.Time.CompareTo(b.Time));
    }

    public void Clear() => _lines.Clear();

    public IReadOnlyList<(TimeSpan Time, string Text)> TimedLines => _lines;

    /// <summary>Index of the line active at <paramref name="currentTime"/>, or -1 if none.</summary>
    public int GetCurrentLineIndex(TimeSpan currentTime)
    {
        if (_lines.Count == 0 || currentTime < TimeSpan.Zero)
            return -1;

        var idx = -1;
        for (var i = 0; i < _lines.Count; i++)
        {
            if (_lines[i].Time <= currentTime)
                idx = i;
            else
                break;
        }

        return idx;
    }

    public (string Current, string Next) GetCurrentAndNextLyrics(TimeSpan currentTime)
    {
        if (_lines.Count == 0 || currentTime < TimeSpan.Zero)
            return (string.Empty, string.Empty);

        string current = string.Empty;
        foreach (var (time, text) in _lines)
        {
            if (time <= currentTime)
            {
                current = text;
                continue;
            }

            return (current, text);
        }

        return (current, string.Empty);
    }

    private static double ParseFractionalSeconds(string digits)
    {
        if (digits.Length == 0)
            return 0d;

        var v = int.Parse(digits, CultureInfo.InvariantCulture);
        return digits.Length >= 3
            ? v / 1000d
            : v / 100d;
    }
}
