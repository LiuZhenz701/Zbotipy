namespace LocalMusicPlayer.Models;

public class Track
{
    public required string FilePath { get; init; }
    public required string Title { get; init; }
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public string? Comment { get; init; }
    public required string DisplayName { get; init; }
}
