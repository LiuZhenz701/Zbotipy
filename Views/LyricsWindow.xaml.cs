using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using LocalMusicPlayer.Services;
using MediaColor = System.Windows.Media.Color;

namespace LocalMusicPlayer.Views;

public partial class LyricsWindow : Window
{
    private HwndSource? _hwndSource;
    private bool _clickThrough;

    private readonly DispatcherTimer _contrastTimer;
    private readonly DispatcherTimer _cursorChromeTimer;
    private bool _chromeControlsVisible;
    private System.Drawing.Bitmap? _backdropScratch;
    private bool _hasContrastState;
    private bool _preferDarkLyricText;
    private bool? _lastAppliedDarkLyricText;

    public LyricsWindow()
    {
        InitializeComponent();

        _contrastTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(280)
        };
        _contrastTimer.Tick += (_, _) => RefreshContrastFromBackdrop();

        _cursorChromeTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _cursorChromeTimer.Tick += (_, _) => UpdateChromeHoverFromCursor();

        Loaded += LyricsWindow_Loaded;
        LocationChanged += (_, _) => RefreshContrastFromBackdrop();
        SizeChanged += LyricsWindow_OnSizeChanged;
        IsVisibleChanged += LyricsWindow_IsVisibleChanged;
        ClickThroughToggle.Checked += ClickThroughToggle_OnAppearanceChanged;
        ClickThroughToggle.Unchecked += ClickThroughToggle_OnAppearanceChanged;
    }

    private void LyricsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _clickThrough = ClickThroughToggle.IsChecked == true;
        SyncLockTogglePresentation();
        RefreshContrastFromBackdrop();
        UpdateChromeHoverFromCursor();
        if (IsVisible)
        {
            _contrastTimer.Start();
            _cursorChromeTimer.Start();
        }
    }

    private void LyricsWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            _hasContrastState = false;
            _lastAppliedDarkLyricText = null;
            _contrastTimer.Start();
            _cursorChromeTimer.Start();
            RefreshContrastFromBackdrop();
            UpdateChromeHoverFromCursor();
        }
        else
        {
            _contrastTimer.Stop();
            _cursorChromeTimer.Stop();
        }
    }

    private void ClickThroughToggle_OnAppearanceChanged(object sender, RoutedEventArgs e)
    {
        ApplyContrastBrushes();
    }

    private void RefreshContrastFromBackdrop()
    {
        if (!IsLoaded || !IsVisible)
            return;

        var hwnd = new WindowInteropHelper(this).Handle;
        var lum = DesktopBackdropSampler.TrySampleEdgeLuminance(hwnd, ref _backdropScratch);
        if (!lum.HasValue)
            return;

        UpdateContrastDecision(lum.Value);
    }

    private void UpdateContrastDecision(double luminance)
    {
        const double hi = 0.52;
        const double lo = 0.42;

        if (!_hasContrastState)
        {
            _preferDarkLyricText = luminance >= 0.5;
            _hasContrastState = true;
        }
        else if (_preferDarkLyricText)
        {
            if (luminance < lo)
                _preferDarkLyricText = false;
        }
        else
        {
            if (luminance > hi)
                _preferDarkLyricText = true;
        }

        if (_preferDarkLyricText != _lastAppliedDarkLyricText)
        {
            _lastAppliedDarkLyricText = _preferDarkLyricText;
            ApplyContrastBrushes();
        }
    }

    private void ApplyContrastBrushes()
    {
        if (_preferDarkLyricText)
        {
            var main = MediaColor.FromRgb(18, 18, 22);
            var next = MediaColor.FromRgb(72, 72, 78);
            CurrentLyricText.Foreground = new SolidColorBrush(main);
            NextLyricText.Foreground = new SolidColorBrush(next);

            var halo = new DropShadowEffect
            {
                Color = Colors.White,
                BlurRadius = 4,
                ShadowDepth = 0,
                Opacity = 0.42
            };
            CurrentLyricText.Effect = halo;
            NextLyricText.Effect = CloneEffect(halo);

            ApplyChrome(false);
        }
        else
        {
            CurrentLyricText.Foreground = System.Windows.Media.Brushes.White;
            NextLyricText.Foreground = new SolidColorBrush(MediaColor.FromRgb(235, 235, 240));

            var shadow = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 4,
                ShadowDepth = 0,
                Opacity = 1
            };
            CurrentLyricText.Effect = shadow;
            NextLyricText.Effect = CloneEffect(shadow);

            ApplyChrome(true);
        }
    }

    private static DropShadowEffect CloneEffect(DropShadowEffect e)
    {
        return new DropShadowEffect
        {
            Color = e.Color,
            BlurRadius = e.BlurRadius,
            ShadowDepth = e.ShadowDepth,
            Opacity = e.Opacity
        };
    }

    private void ApplyChrome(bool lightTextOnDarkBackdrop)
    {
        var chromeShadow = new DropShadowEffect
        {
            Color = lightTextOnDarkBackdrop ? Colors.Black : Colors.White,
            BlurRadius = 3,
            ShadowDepth = 0,
            Opacity = lightTextOnDarkBackdrop ? 1 : 0.4
        };

        CloseLyricsButton.Effect = CloneEffect(chromeShadow);

        if (lightTextOnDarkBackdrop)
        {
            CloseLyricsButton.Foreground = System.Windows.Media.Brushes.White;
            if (ClickThroughToggle.IsChecked == true)
                ClickThroughToggle.Foreground = new SolidColorBrush(MediaColor.FromRgb(160, 210, 255));
            else
                ClickThroughToggle.Foreground = System.Windows.Media.Brushes.White;
        }
        else
        {
            CloseLyricsButton.Foreground = new SolidColorBrush(MediaColor.FromRgb(28, 28, 32));
            if (ClickThroughToggle.IsChecked == true)
                ClickThroughToggle.Foreground = new SolidColorBrush(MediaColor.FromRgb(20, 70, 160));
            else
                ClickThroughToggle.Foreground = new SolidColorBrush(MediaColor.FromRgb(40, 40, 44));
        }

        ClickThroughToggle.Effect = CloneEffect(chromeShadow);

        ApplyTransportChrome(lightTextOnDarkBackdrop, chromeShadow);
    }

    private void ApplyTransportChrome(bool lightTextOnDarkBackdrop, DropShadowEffect chromeShadow)
    {
        PrevButton.Effect = CloneEffect(chromeShadow);
        PlayPauseButton.Effect = CloneEffect(chromeShadow);
        NextButton.Effect = CloneEffect(chromeShadow);

        if (lightTextOnDarkBackdrop)
        {
            PrevButton.Foreground = System.Windows.Media.Brushes.White;
            PlayPauseButton.Foreground = System.Windows.Media.Brushes.White;
            NextButton.Foreground = System.Windows.Media.Brushes.White;
        }
        else
        {
            var fg = new SolidColorBrush(MediaColor.FromRgb(28, 28, 32));
            PrevButton.Foreground = fg;
            PlayPauseButton.Foreground = fg;
            NextButton.Foreground = fg;
        }
    }

    private void LyricsWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RefreshContrastFromBackdrop();
    }

    private void LyricText_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_clickThrough)
            return;

        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void ToolbarRow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Controls.Primitives.ButtonBase)
            return;

        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void ClickThroughToggle_OnChecked(object sender, RoutedEventArgs e)
    {
        _clickThrough = true;
        SyncLockTogglePresentation();
    }

    private void ClickThroughToggle_OnUnchecked(object sender, RoutedEventArgs e)
    {
        _clickThrough = false;
        SyncLockTogglePresentation();
    }

    /// <summary>
    /// Unlocked when the lyric area can be used to drag; locked when click-through is on (drag only from this bar / close).
    /// </summary>
    private void UpdateChromeHoverFromCursor()
    {
        if (!IsLoaded || !IsVisible)
            return;

        bool inside = IsScreenCursorInWindowBounds();
        if (inside == _chromeControlsVisible)
            return;

        _chromeControlsVisible = inside;
        var v = inside ? Visibility.Visible : Visibility.Collapsed;
        CloseHitArea.Visibility = v;
        BottomToolbarChromePanel.Visibility = v;
    }

    private bool IsScreenCursorInWindowBounds()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return false;

        if (!GetWindowRect(hwnd, out var rc))
            return false;

        if (!GetCursorPos(out var pt))
            return false;

        return pt.X >= rc.Left && pt.X < rc.Right && pt.Y >= rc.Top && pt.Y < rc.Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINRECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out WINRECT lpRect);

    private void SyncLockTogglePresentation()
    {
        if (_clickThrough)
        {
            ClickThroughToggle.Content = "\U0001F512";
            ClickThroughToggle.ToolTip =
                "Locked: clicks pass through the lyric area. Drag using this bar, or tap to unlock and drag from the lyrics.";
        }
        else
        {
            ClickThroughToggle.Content = "\U0001F513";
            ClickThroughToggle.ToolTip =
                "Unlocked: drag the lyric area to move. Tap to lock and pass clicks through to windows below.";
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Hide();
        if (System.Windows.Application.Current?.MainWindow is MainWindow mw)
            mw.RefreshFloatingLyricsButtonPresentation();
    }

    protected override void OnClosed(EventArgs e)
    {
        _contrastTimer.Stop();
        _cursorChromeTimer.Stop();
        _backdropScratch?.Dispose();
        _backdropScratch = null;

        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }

        base.OnClosed(e);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        _hwndSource?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084;
        const int HTTRANSPARENT = -1;
        const int HTCLIENT = 1;

        if (msg != WM_NCHITTEST || !_clickThrough || !IsLoaded)
            return IntPtr.Zero;

        var screen = LParamToScreenPoint(lParam);
        var p = PointFromScreen(screen);

        if (IsPointInClientChrome(p))
        {
            handled = true;
            return (IntPtr)HTCLIENT;
        }

        handled = true;
        return (IntPtr)HTTRANSPARENT;
    }

    private static System.Windows.Point LParamToScreenPoint(IntPtr lParam)
    {
        var v = lParam.ToInt64();
        var x = (int)(short)(v & 0xFFFF);
        var y = (int)(short)((v >> 16) & 0xFFFF);
        return new System.Windows.Point(x, y);
    }

    private bool IsPointInClientChrome(System.Windows.Point windowPoint)
    {
        return IsPointInElement(ToolbarRow, windowPoint)
               || IsPointInElement(CloseHitArea, windowPoint);
    }

    private bool IsPointInElement(FrameworkElement element, System.Windows.Point windowPoint)
    {
        if (element.Visibility != Visibility.Visible)
            return false;

        var w = element.ActualWidth;
        var h = element.ActualHeight;
        if (w <= 0 || h <= 0)
            return false;

        var origin = element.TransformToVisual(this).Transform(new System.Windows.Point(0, 0));
        return windowPoint.X >= origin.X && windowPoint.X <= origin.X + w &&
               windowPoint.Y >= origin.Y && windowPoint.Y <= origin.Y + h;
    }
}
