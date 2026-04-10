using System.IO;
using NAudio.Wave;

namespace LocalMusicPlayer.Services;

public sealed class AudioPlayerService : IDisposable
{
    private readonly object _sync = new();
    private AudioFileReader? _reader;
    private WaveOutEvent? _waveOut;
    private bool _disposed;
    private bool _ignoreNextPlaybackStopped;

    public event EventHandler? PlaybackEnded;

    public TimeSpan CurrentTime
    {
        get
        {
            lock (_sync)
            {
                return _reader?.CurrentTime ?? TimeSpan.Zero;
            }
        }
    }

    public TimeSpan TotalTime
    {
        get
        {
            lock (_sync)
            {
                return _reader?.TotalTime ?? TimeSpan.Zero;
            }
        }
    }

    public bool IsPlaying
    {
        get
        {
            lock (_sync)
            {
                return _waveOut?.PlaybackState == PlaybackState.Playing;
            }
        }
    }

    public void Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found.", filePath);

        lock (_sync)
        {
            ThrowIfDisposed();
            ReleasePlaybackLocked();

            var reader = new AudioFileReader(filePath);
            var waveOut = new WaveOutEvent();
            try
            {
                waveOut.Init(reader);
                waveOut.PlaybackStopped += OnPlaybackStopped;
            }
            catch
            {
                waveOut.Dispose();
                reader.Dispose();
                throw;
            }

            _reader = reader;
            _waveOut = waveOut;
        }
    }

    public void Play()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            EnsureLoaded();
            _waveOut!.Play();
        }
    }

    public void Pause()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            EnsureLoaded();
            _waveOut!.Pause();
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            EnsureLoaded();
            _ignoreNextPlaybackStopped = true;
            _waveOut!.Stop();
            _reader!.CurrentTime = TimeSpan.Zero;
        }
    }

    public void Seek(TimeSpan position)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            EnsureLoaded();

            var total = _reader!.TotalTime;
            if (position < TimeSpan.Zero)
                position = TimeSpan.Zero;
            else if (position > total)
                position = total;

            _reader.CurrentTime = position;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            ReleasePlaybackLocked();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    private void EnsureLoaded()
    {
        if (_reader is null || _waveOut is null)
            throw new InvalidOperationException("No audio loaded. Call Load first.");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void ReleasePlaybackLocked()
    {
        if (_waveOut != null)
        {
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }

        if (_reader != null)
        {
            _reader.Dispose();
            _reader = null;
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
            return;

        lock (_sync)
        {
            if (_disposed)
                return;

            if (_ignoreNextPlaybackStopped)
            {
                _ignoreNextPlaybackStopped = false;
                return;
            }
        }

        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }
}
