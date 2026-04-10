using System.Collections.ObjectModel;

namespace LocalMusicPlayer.Models;

public class Playlist
{
    public required string Name { get; init; }
    public ObservableCollection<Track> Tracks { get; } = new();
}
