using System.IO;

namespace LocalMusicPlayer.Services;

public static class PlaylistLibrary
{
    public static string RootPath =>
        Path.Combine(@"F:\Documents\MyWorks\MusicPlayer", "Playlist");

    public static void EnsureRootExists() => Directory.CreateDirectory(RootPath);

    public static void CopyFromSourceFolder(string sourceRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);
        if (!Directory.Exists(sourceRoot))
            return;

        EnsureRootExists();
        var rootDir = new DirectoryInfo(sourceRoot);
        var subDirs = rootDir
            .EnumerateDirectories()
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (subDirs.Count > 0)
        {
            foreach (var sub in subDirs)
            {
                var destDir = Path.Combine(RootPath, sub.Name);
                CopyMp3AndLrcFromDirectory(sub.FullName, destDir);
            }
        }
        else
        {
            var destDir = Path.Combine(RootPath, rootDir.Name);
            CopyMp3AndLrcFromDirectory(sourceRoot, destDir);
        }
    }

    private static void CopyMp3AndLrcFromDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var pattern in new[] { "*.mp3", "*.lrc" })
        {
            foreach (var path in Directory
                         .EnumerateFiles(sourceDir, pattern, SearchOption.TopDirectoryOnly)
                         .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                var dest = Path.Combine(destDir, Path.GetFileName(path));
                try
                {
                    File.Copy(path, dest, overwrite: true);
                }
                catch (IOException)
                {
                    // Locked or transient IO — skip this file
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}
