using System.Windows.Forms.Integration;
using MediaElement = System.Windows.Controls.MediaElement;
using MediaState = System.Windows.Controls.MediaState;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfStretch = System.Windows.Media.Stretch;

namespace RE4_PS2_MOD_WORKSPACE;

/// <summary>Embedded MPEG-1 + decoded ADX player backed by Windows Media Foundation.</summary>
public sealed class SfdVideoPlayerControl : UserControl
{
    private readonly ElementHost host;
    private readonly MediaElement video;
    private readonly System.Windows.Media.MediaPlayer audio = new();
    private readonly System.Windows.Threading.DispatcherTimer thumbnailTimer;
    private string? videoPath, audioPath;
    private bool loaded, videoReady, audioReady, pendingPlay, pendingThumbnail;

    public event EventHandler<string>? PlaybackFailed;
    public bool IsLoaded => loaded;
    public bool IsPlaying { get; private set; }
    public string? SourceVideoPath => videoPath;
    public TimeSpan Position => loaded ? audio.Position : TimeSpan.Zero;

    public double Volume
    {
        get => audio.Volume;
        set { double volume = Math.Clamp(value, 0, 1); audio.Volume = volume; video.Volume = 0; }
    }

    public SfdVideoPlayerControl()
    {
        BackColor = Color.Black;
        video = new MediaElement
        {
            LoadedBehavior = MediaState.Manual,
            UnloadedBehavior = MediaState.Manual,
            Stretch = WpfStretch.Uniform,
            ScrubbingEnabled = true,
            Volume = 0
        };
        thumbnailTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
        thumbnailTimer.Tick += (_, _) => { thumbnailTimer.Stop(); if (!pendingPlay && !IsPlaying) { video.Pause(); video.Position = TimeSpan.Zero; } pendingThumbnail = false; };
        video.MediaOpened += (_, _) => { videoReady = true; if (pendingThumbnail) StartThumbnail(); StartPendingPlayback(); };
        video.MediaFailed += (_, e) => PlaybackFailed?.Invoke(this, e.ErrorException?.Message ?? "O Windows não conseguiu decodificar o vídeo MPEG-1.");
        audio.MediaOpened += (_, _) => { audioReady = true; StartPendingPlayback(); };
        audio.MediaFailed += (_, e) => PlaybackFailed?.Invoke(this, e.ErrorException?.Message ?? "O Windows não conseguiu abrir o áudio WAV temporário.");
        var border = new System.Windows.Controls.Border { Background = WpfBrushes.Black, Child = video };
        host = new ElementHost { Dock = DockStyle.Fill, BackColor = Color.Black, Child = border };
        Controls.Add(host);
    }

    public void LoadMedia(string mpegVideoPath, string waveAudioPath)
    {
        UnloadMedia();
        videoPath = Path.GetFullPath(mpegVideoPath); audioPath = Path.GetFullPath(waveAudioPath);
        video.Source = new Uri(videoPath, UriKind.Absolute);
        audio.Open(new Uri(audioPath, UriKind.Absolute));
        video.Position = TimeSpan.Zero; audio.Position = TimeSpan.Zero; video.Volume = 0;
        loaded = true; videoReady = audioReady = pendingPlay = pendingThumbnail = false;
    }

    public void ShowFirstFrame()
    {
        if (!loaded) return;
        pendingThumbnail = true;
        if (videoReady) StartThumbnail(); else video.Play();
    }

    private void StartThumbnail()
    {
        if (!loaded || pendingPlay || IsPlaying) return;
        video.Position = TimeSpan.Zero; video.Play(); thumbnailTimer.Stop(); thumbnailTimer.Start();
    }

    public void Play()
    {
        if (!loaded) return;
        thumbnailTimer.Stop(); pendingThumbnail = false;
        pendingPlay = true;
        if (!videoReady) { video.Play(); video.Pause(); }
        StartPendingPlayback();
    }

    private void StartPendingPlayback()
    {
        if (!loaded || !pendingPlay || !videoReady || !audioReady) return;
        pendingPlay = false; TimeSpan position = audio.Position; video.Position = position;
        video.Play(); audio.Play(); IsPlaying = true;
    }

    public void Pause()
    {
        if (!loaded) return;
        pendingPlay = false; audio.Pause(); video.Pause(); IsPlaying = false;
        video.Position = audio.Position;
    }

    public void StopPlayback()
    {
        if (!loaded) return;
        pendingPlay = false; audio.Stop(); video.Stop(); video.Position = TimeSpan.Zero; audio.Position = TimeSpan.Zero; IsPlaying = false;
        ShowFirstFrame();
    }

    public void Seek(TimeSpan position)
    {
        if (!loaded) return;
        bool resume = IsPlaying; audio.Pause(); video.Pause();
        audio.Position = position; video.Position = position;
        if (resume) { video.Play(); audio.Play(); }
    }

    public void UnloadMedia()
    {
        thumbnailTimer.Stop(); IsPlaying = false; loaded = videoReady = audioReady = pendingPlay = pendingThumbnail = false;
        try { audio.Stop(); audio.Close(); } catch { }
        try { video.Stop(); video.Close(); video.Source = null; } catch { }
        videoPath = audioPath = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { UnloadMedia(); audio.Close(); host.Dispose(); }
        base.Dispose(disposing);
    }
}
