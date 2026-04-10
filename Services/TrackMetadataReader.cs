using System.IO;
using LocalMusicPlayer.Models;

namespace LocalMusicPlayer.Services;

public static class TrackMetadataReader
{
    public static Track Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileName = Path.GetFileNameWithoutExtension(path);
        string? title = null;
        string? artist = null;
        string? album = null;
        string? comment = null;

        try
        {
            using var file = TagLib.File.Create(path);
            var tag = file.Tag;
            if (!string.IsNullOrWhiteSpace(tag.Title))
                title = tag.Title.Trim();

            var performers = tag.Performers;
            if (performers is { Length: > 0 })
            {
                var parts = performers
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToArray();

                if (parts.Length > 0)
                    artist = string.Join(", ", parts);
            }

            if (!string.IsNullOrWhiteSpace(tag.Album))
                album = tag.Album.Trim();

            if (!string.IsNullOrWhiteSpace(tag.Comment))
                comment = tag.Comment.Trim();
        }
        catch (Exception)
        {
            // Corrupt or locked file: fall back to filename only
        }

        var resolvedTitle = title ?? fileName;
        var displayName = BuildDisplayName(resolvedTitle, artist, album);

        return new Track
        {
            FilePath = path,
            Title = resolvedTitle,
            Artist = artist,
            Album = album,
            Comment = comment,
            DisplayName = displayName
        };
    }

    private static string BuildDisplayName(string title, string? artist, string? album)
    {
        if (!string.IsNullOrEmpty(artist))
            return $"{title} — {artist}";

        if (!string.IsNullOrEmpty(album))
            return $"{title} · {album}";

        return title;
    }
}
