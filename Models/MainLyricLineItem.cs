namespace LocalMusicPlayer.Models;

/// <summary>One synced lyric line for the main window (Spotify-style list).</summary>
public sealed class MainLyricLineItem
{
    public MainLyricLineItem(int lineIndex, string text)
    {
        LineIndex = lineIndex;
        Text = text;
    }

    public int LineIndex { get; }
    public string Text { get; }
}
