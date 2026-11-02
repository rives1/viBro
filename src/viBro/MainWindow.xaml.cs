using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using ViBro.Mpv;
using ViBro.Util;

namespace ViBro;

public partial class MainWindow : Window
{
    private readonly StartupOptions _options;
    private readonly MetricsLog _metrics;
    private readonly DispatcherTimer _uiTimer;
    private readonly RollingStats _seekStats = new(50);

    private MpvPlayer? _player;
    private List<VideoItem> _items = new();
    private string? _currentFolder;
    private CancellationTokenSource? _folderCts;
    private int _folderGeneration;
    private MediaInfo? _currentInfo;
    private VideoItem? _loadingItem;

    private bool _exactSeeks;
    private bool _updatingTimeSlider;
    private string _lastLoadText = "";
    private string _lastSeekText = "";

    private bool _isFullscreen;
    private WindowState _savedState;
    private WindowStyle _savedStyle;
    private ResizeMode _savedResizeMode;
    private GridLength _savedListHeight;
    private double _savedListMinHeight;

    internal MainWindow(StartupOptions options)
    {
        _options = options;
        InitializeComponent();

        Title = "viBro " + AppInfo.DisplayVersion;
        _metrics = new MetricsLog(Path.Combine(AppInfo.ExeDirectory, "viBro-metrics.csv"));
        _exactSeeks = options.ExactSeeks;
        ExactSeekBox.IsChecked = options.ExactSeeks;

        _uiTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(100) };
        _uiTimer.Tick += (_, _) => RefreshTransport();

        // Keys pressed while the native video window has focus never reach WPF's
        // routed events: intercept them here and route them to the same handler.
        ComponentDispatcher.ThreadPreprocessMessage += OnThreadPreprocessMessage;
    }

    // ================================================================ lifecycle

    private void Window_ContentRendered(object? sender, EventArgs e)
    {
        if (_player != null) return;

        try
        {
            IntPtr wid = VideoHost.Handle;
            if (wid == IntPtr.Zero) throw new InvalidOperationException("Video host window was not created.");

            _player = new MpvPlayer(wid, BuildMpvOptions());
            _player.LoadCompleted += r => Dispatcher.InvokeAsync(() => OnLoadCompleted(r));
            _player.LoadFailed += (path, err) => Dispatcher.InvokeAsync(() => OnLoadFailed(path, err));
            _player.SeekCompleted += r => Dispatcher.InvokeAsync(() => OnSeekCompleted(r));
            _player.Start();

            var warnings = _options.Errors.Concat(_player.OptionErrors).ToList();
            SetStatus(warnings.Count == 0
                ? $"Video engine ready (libmpv API {_player.ApiVersion})"
                : "Option problems: " + string.Join("; ", warnings));
        }
        catch (Exception ex)
        {
            _player = null;
            MessageBox.Show(this,
                "The video engine could not be started.\n\n" + ex.Message +
                "\n\nCheck that viBro.exe was built with libmpv-2.dll (see README).",
                "viBro", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Video engine not available");
        }

        _uiTimer.Start();

        if (!string.IsNullOrWhiteSpace(_options.Folder))
            OpenFolder(_options.Folder!);
        else
            PathBox.Text = "";
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _uiTimer.Stop();
        ComponentDispatcher.ThreadPreprocessMessage -= OnThreadPreprocessMessage;
        _folderCts?.Cancel();
        _player?.Dispose();
        _player = null;
    }

    private IEnumerable<KeyValuePair<string, string>> BuildMpvOptions()
    {
        // Isolation from any mpv installed on the machine, no subtitles, no mpv UI or key bindings.
        var o = new List<KeyValuePair<string, string>>
        {
            new("config", "no"),
            new("load-scripts", "no"),
            new("ytdl", "no"),
            new("terminal", "no"),
            new("osc", "no"),
            new("input-default-bindings", "no"),
            new("input-vo-keyboard", "no"),
            new("input-cursor", "no"),
            new("sid", "no"),
            new("sub-auto", "no"),
            new("audio-display", "no"),
            // Keep one engine and one video window alive between files.
            new("idle", "yes"),
            new("force-window", "yes"),
            new("keep-open", "yes"),
            new("pause", "yes"),
            // Hardware decoding where safe (D3D11VA, etc.), automatic software fallback.
            new("hwdec", SelectedHwdec()),
            // No large read-ahead on network shares; tune with --mpv-opt during tests.
            new("cache", "no"),
        };
        o.AddRange(_options.MpvOptions); // command-line overrides win (applied last)
        return o;
    }

    // ================================================================ folder

    private async void OpenFolder(string rawPath)
    {
        string path = rawPath.Trim().Trim('"');
        if (path.Length == 0) return;
        try
        {
            path = Path.GetFullPath(path); // string-only, no I/O
        }
        catch (Exception ex)
        {
            SetStatus("Invalid path: " + ex.Message);
            return;
        }

        int generation = ++_folderGeneration;
        _folderCts?.Cancel();
        var cts = new CancellationTokenSource();
        _folderCts = cts;

        PathBox.Text = path;
        SetStatus("Opening " + path + " ...");

        var scan = Task.Run(() => FolderScanner.Scan(path, cts.Token));

        // Tell the user when a share is slow to answer; the UI stays usable meanwhile.
        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            if (!scan.IsCompleted && generation == _folderGeneration)
                SetStatus("Still waiting for " + path + " (network location not responding?)");
        }, TaskScheduler.FromCurrentSynchronizationContext());

        try
        {
            var items = await scan;
            if (generation != _folderGeneration) return; // superseded by a newer request

            _currentFolder = path;
            _items = items;
            FileList.ItemsSource = _items;
            SetStatus($"{items.Count} video file(s) in {path}");
        }
        catch (OperationCanceledException)
        {
            // superseded
        }
        catch (Exception ex)
        {
            if (generation == _folderGeneration)
                SetStatus("Cannot open folder: " + ex.Message);
        }
    }

    private void PathBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OpenFolder(PathBox.Text);
            Keyboard.Focus(FileList);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            PathBox.Text = _currentFolder ?? "";
            Keyboard.Focus(FileList);
            e.Handled = true;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Select a folder with videos" };
        if (!string.IsNullOrEmpty(_currentFolder)) dialog.InitialDirectory = _currentFolder;
        if (dialog.ShowDialog(this) == true)
            OpenFolder(dialog.FolderName);
    }

    private void FocusPathBox()
    {
        PathBox.Focus();
        PathBox.SelectAll();
    }

    // ================================================================ selection and loading

    private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FileList.SelectedItem is VideoItem item)
            LoadItem(item);
    }

    private void LoadItem(VideoItem item)
    {
        if (_player == null) return;
        _loadingItem = item;
        _currentInfo = null;
        SetStatus("Loading " + item.Name + " ...");
        _player.Load(item.FullPath, item);
    }

    /// <summary>E / D: previous / next file, loaded paused. No wrap-around at the ends.</summary>
    private void SelectRelative(int direction)
    {
        if (_items.Count == 0) return;
        int index = FileList.SelectedIndex;
        int next = index < 0 ? (direction > 0 ? 0 : _items.Count - 1) : index + direction;
        if (next < 0 || next >= _items.Count) return;
        FileList.SelectedIndex = next;
        FileList.ScrollIntoView(_items[next]);
    }

    private void OnLoadCompleted(LoadResult r)
    {
        if (r.Tag is not VideoItem item || !ReferenceEquals(item, _loadingItem)) return;
        _currentInfo = r.Info;
        string res = r.Info.Width > 0 ? $"{r.Info.Width}x{r.Info.Height}" : "no video";
        SetStatus($"{item.Name}  |  {r.Info.Codec} {res}  |  hwdec: {Display(r.Info.Hwdec)}");
        _lastLoadText = $"{r.Kind} {r.ElapsedMs:0} ms";
        UpdateMetricsText();

        RecordMetric(r.Kind, r.ElapsedMs, item, seekMode: "", delta: null, target: null, coalesced: 0);
    }

    private void OnLoadFailed(string path, string error)
    {
        SetStatus($"Cannot play {Path.GetFileName(path)}: {error}");
    }

    // ================================================================ seeking and transport

    private void Seek(double deltaSeconds) => _player?.SeekRelative(deltaSeconds, _exactSeeks);

    private void OnSeekCompleted(SeekResult r)
    {
        if (r.Tag is not VideoItem item) return;
        string kind = r.TimedOut ? "seek_timeout" : "seek";
        if (!r.TimedOut) _seekStats.Add(r.ElapsedMs);
        _lastSeekText = $"seek {r.ElapsedMs:0} ms (median {_seekStats.Median:0}, n={_seekStats.Count})";
        UpdateMetricsText();

        RecordMetric(kind, r.ElapsedMs, item, r.Exact ? "exact" : "keyframes", r.Delta, r.Target, r.Coalesced);
    }

    private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingTimeSlider || _player == null) return;
        _player.SeekAbsolute(e.NewValue, _exactSeeks); // coalesced while dragging
    }

    private void RefreshTransport()
    {
        if (_player == null) return;
        double duration = _player.Duration;
        double position = Math.Min(_player.TimePos, Math.Max(duration, 0));

        TimeText.Text = FormatTime(position) + " / " + FormatTime(duration);
        PlayButton.Content = _player.IsPaused ? "Play" : "Pause";
        MuteButton.Content = _player.IsMuted ? "Unmute" : "Mute";

        if (!TimeSlider.IsMouseCaptureWithin)
        {
            _updatingTimeSlider = true;
            TimeSlider.Maximum = duration > 0 ? duration : 1;
            TimeSlider.Value = Math.Clamp(position, 0, TimeSlider.Maximum);
            _updatingTimeSlider = false;
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e) => _player?.TogglePause();

    private void MuteButton_Click(object sender, RoutedEventArgs e) => _player?.ToggleMute();

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        => _player?.SetVolume(e.NewValue);

    private void ExactSeekBox_Changed(object sender, RoutedEventArgs e)
        => _exactSeeks = ExactSeekBox.IsChecked == true;

    private void HwdecBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => _player?.SetHwdec(SelectedHwdec());

    private string SelectedHwdec()
        => (HwdecBox?.SelectedItem as ComboBoxItem)?.Content as string ?? "auto-safe";

    // ================================================================ keyboard

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Shortcuts are suspended while a text field is being edited.
        if (Keyboard.FocusedElement is TextBoxBase) return;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HandleShortcut(key, Keyboard.Modifiers, e.IsRepeat))
            e.Handled = true;
    }

    private void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        if (handled) return;
        if (msg.message != NativeMethods.WM_KEYDOWN && msg.message != NativeMethods.WM_SYSKEYDOWN) return;

        IntPtr host = VideoHost.Handle;
        if (host == IntPtr.Zero) return;
        if (msg.hwnd != host && !NativeMethods.IsChild(host, msg.hwnd)) return;

        Key key = KeyInterop.KeyFromVirtualKey((int)msg.wParam.ToInt64());
        bool isRepeat = (msg.lParam.ToInt64() & 0x40000000) != 0;
        if (HandleShortcut(key, NativeMethods.CurrentModifiers(), isRepeat))
            handled = true;
    }

    private bool HandleShortcut(Key key, ModifierKeys mods, bool isRepeat)
    {
        if (!IsActive) return false; // e.g. a modal dialog is open

        bool alt = (mods & ModifierKeys.Alt) != 0;
        bool ctrl = (mods & ModifierKeys.Control) != 0;

        switch (key)
        {
            case Key.Left when !alt && !ctrl: Seek(-1); return true;
            case Key.Right when !alt && !ctrl: Seek(+1); return true;
            case Key.Down when !alt && !ctrl: Seek(-5); return true;
            case Key.Up when !alt && !ctrl: Seek(+5); return true;
            case Key.PageDown when !ctrl: Seek(alt ? -300 : -60); return true;
            case Key.PageUp when !ctrl: Seek(alt ? +300 : +60); return true;

            case Key.Space when !alt && !ctrl:
                if (!isRepeat) _player?.TogglePause();
                return true;

            case Key.E when !alt && !ctrl: SelectRelative(-1); return true;
            case Key.D when !alt && !ctrl: SelectRelative(+1); return true;

            case Key.M when !alt && !ctrl:
                if (!isRepeat) _player?.ToggleMute();
                return true;

            case Key.F11 when !alt && !ctrl:
                if (!isRepeat) ToggleFullscreen();
                return true;

            case Key.Escape when _isFullscreen:
                ExitFullscreen();
                return true;

            case Key.L when ctrl && !alt:
                FocusPathBox();
                return true;
        }
        return false;
    }

    // ================================================================ fullscreen

    private void ToggleFullscreen()
    {
        if (_isFullscreen) ExitFullscreen();
        else EnterFullscreen();
    }

    private void EnterFullscreen()
    {
        _isFullscreen = true;
        _savedState = WindowState;
        _savedStyle = WindowStyle;
        _savedResizeMode = ResizeMode;
        var listRow = MainArea.RowDefinitions[2];
        _savedListHeight = listRow.Height;
        _savedListMinHeight = listRow.MinHeight;

        TopBar.Visibility = Visibility.Collapsed;
        TransportBar.Visibility = Visibility.Collapsed;
        StatusPanel.Visibility = Visibility.Collapsed;
        ListSplitter.Visibility = Visibility.Collapsed;
        FileList.Visibility = Visibility.Collapsed;
        listRow.MinHeight = 0;
        listRow.Height = new GridLength(0);

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
        WindowState = WindowState.Maximized;
    }

    private void ExitFullscreen()
    {
        _isFullscreen = false;
        WindowStyle = _savedStyle;
        ResizeMode = _savedResizeMode;
        WindowState = _savedState == WindowState.Minimized ? WindowState.Normal : _savedState;

        var listRow = MainArea.RowDefinitions[2];
        listRow.Height = _savedListHeight;
        listRow.MinHeight = _savedListMinHeight;
        TopBar.Visibility = Visibility.Visible;
        TransportBar.Visibility = Visibility.Visible;
        StatusPanel.Visibility = Visibility.Visible;
        ListSplitter.Visibility = Visibility.Visible;
        FileList.Visibility = Visibility.Visible;
    }

    // ================================================================ metrics and status

    private void RecordMetric(string kind, double ms, VideoItem item, string seekMode,
        double? delta, double? target, int coalesced)
    {
        var mem = MetricsLog.MemorySnapshot();
        var info = _currentInfo;
        _metrics.Append(new MetricRecord(
            DateTime.UtcNow, kind, ms, item.Name, item.Size, item.Storage, seekMode, delta, target, coalesced,
            info?.Codec, info?.Width ?? 0, info?.Height ?? 0, info?.Hwdec, info?.Vo,
            mem.WorkingSetMb, mem.PrivateMb, mem.ManagedMb, AppInfo.SemanticVersion));

        if (_metrics.LastError != null)
            SetStatus("Metrics not saved: " + _metrics.LastError);
    }

    private void UpdateMetricsText()
    {
        var mem = MetricsLog.MemorySnapshot();
        MetricsText.Text = string.Join("  |  ",
            new[] { _lastLoadText, _lastSeekText, $"mem {mem.PrivateMb:0} MB" }.Where(s => s.Length > 0));
    }

    private void SetStatus(string text) => StatusText.Text = text;

    private static string Display(string? s) => string.IsNullOrEmpty(s) ? "-" : s;

    private static string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || seconds < 0) seconds = 0;
        var t = TimeSpan.FromSeconds(Math.Floor(seconds));
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }
}
