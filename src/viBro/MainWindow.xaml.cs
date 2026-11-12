using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using ViBro.Settings;
using ViBro.Tree;
using ViBro.Util;

namespace ViBro;

public partial class MainWindow : Window
{
    private readonly StartupOptions _options;
    private readonly AppSettings _settings;
    private readonly string _iniPath;
    private readonly bool _iniWritable;
    private readonly MetricsLog? _metrics;
    private readonly DispatcherTimer _uiTimer;
    private readonly RollingStats _seekStats = new(50);
    private readonly FolderTreeModel _tree;
    private readonly List<string> _warnings = new();
    private int _loggedWarnings;

    private MpvPlayer? _player;
    private List<VideoItem> _items = new();
    private string? _currentFolder;
    private CancellationTokenSource? _folderCts;
    private int _folderGeneration;
    private MediaInfo? _currentInfo;
    private VideoItem? _loadingItem;

    // Folder tree
    private CancellationTokenSource? _revealCts;
    private FolderNode? _revealedNode;   // node selected by code, not by the user
    private FolderNode? _treeSelected;

    // File list
    private SortColumn _sortColumn;
    private bool _sortDescending;
    private bool _suppressSelectionLoad;

    // Seeking: steps shorter than the threshold are always exact (keyframe seeks would not move the picture).
    private readonly double _exactThreshold;
    private readonly bool _exactLongSeeks;
    private bool _updatingTimeSlider;
    private bool _win32KeyRepeat; // auto-repeat flag of the key message being processed
    private string _lastLoadText = "";
    private string _lastSeekText = "";

    // Fullscreen
    private bool _isFullscreen;
    private readonly DispatcherTimer _fullscreenBarTimer;
    private int _lastMouseX = int.MinValue, _lastMouseY = int.MinValue;
    private WindowState _savedState;
    private WindowStyle _savedStyle;
    private ResizeMode _savedResizeMode;
    private GridLength _savedListHeight, _savedTreeWidth;
    private double _savedListMinHeight, _savedTreeMinWidth;

    internal MainWindow(StartupOptions options)
    {
        _options = options;

        // Settings next to the EXE. If they cannot be saved there, warn and keep them in memory only.
        _iniPath = Path.Combine(AppInfo.ExeDirectory, AppSettings.FileName);
        _settings = AppSettings.Load(_iniPath);
        ThemeManager.Apply(_settings.Theme); // before the controls are created

        InitializeComponent();
        ThemeManager.AttachWindow(this);
        ApplyItemStyles();
        PathIcon.Source = TreeIcons.Folder; // decorative, as in the tree
        SliderDrag.Enable(TimeSlider);
        SliderDrag.Enable(VolumeSlider);
        StatusPanel.SetResourceReference(BackgroundProperty, ThemeManager.PanelKey);
        StatusPanel.SetResourceReference(ForegroundProperty, ThemeManager.ForegroundKey);
        Title = "viBro " + AppInfo.DisplayVersion;
        UpdateThemeButton();
        _warnings.AddRange(_settings.Warnings);
        _iniWritable = AppSettings.CanWrite(_iniPath, out string? reason);
        if (!_iniWritable)
            _warnings.Add($"Settings will not be saved: {AppSettings.FileName} cannot be written ({reason}).");

        _exactThreshold = _settings.ExactSeekThresholdSeconds;
        _exactLongSeeks = _settings.ExactLongSeeks || options.ExactSeeks; // the command line is not persisted
        _sortColumn = _settings.SortColumn;
        _sortDescending = _settings.SortDescending;
        _metrics = _settings.MetricsLog
            ? new MetricsLog(Path.Combine(AppInfo.ExeDirectory, "viBro-metrics.csv"))
            : null;

        ApplyWindowSettings();
        UpdateSortHeaders();

        _tree = FolderTreeModel.CreateDefault();
        FolderTree.ItemsSource = _tree.Roots;

        _uiTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(100) };
        _uiTimer.Tick += (_, _) => RefreshTransport();
        _fullscreenBarTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _fullscreenBarTimer.Tick += (_, _) => HideFullscreenBarIfIdle();
        SizeChanged += (_, _) => { if (_isFullscreen) SizeFullscreenBar(); };

        // Keys pressed while the native video window has focus never reach WPF's
        // routed events: intercept them here and route them to the same handler.
        ComponentDispatcher.ThreadPreprocessMessage += OnThreadPreprocessMessage;
        Activated += (_, _) => RecordActivation(); // the activating click must not Play/Pause
        Deactivated += (_, _) => RestoreCursor(); // never leave the cursor hidden for other windows

        // Status bar: number of videos in the list and their total size, whenever the list changes.
        System.ComponentModel.DependencyPropertyDescriptor
            .FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListView))
            .AddValueChanged(FileList, (_, _) => UpdateFolderSummary());
        UpdateFolderSummary();

        ShowWarnings();
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
            _player.EngineMessage += (level, module, text) => ErrorLog.Write("mpv", $"{level} [{module}] {text}");
            _player.PlaybackFailed += err => Dispatcher.InvokeAsync(() => OnPlaybackFailed(err));
            _player.Start();

            _warnings.AddRange(_options.Errors);
            _warnings.AddRange(_player.OptionErrors.Select(o => "mpv option: " + o));
            ShowWarnings();
            SetStatus($"Video engine ready (libmpv API {_player.ApiVersion})");
            UpdateVideoPlaceholder(); // nothing loaded yet: the guide text instead of the empty video window
        }
        catch (Exception ex)
        {
            _player = null;
            ErrorLog.Write("engine", "Video engine could not be started", ex);
            MessageBox.Show(this,
                "The video engine could not be started.\n\n" + ex.Message +
                "\n\nCheck that viBro.exe was built with libmpv-2.dll (see README).",
                "viBro", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Video engine not available", StatusKind.Error);
            VideoPlaceholder.Text = "Video engine not available";
            VideoHost.Visibility = Visibility.Hidden; // so the text is visible
        }

        _uiTimer.Start();
        RecordStartup();

        // Restore only the last folder: no video is loaded automatically.
        string? folder = !string.IsNullOrWhiteSpace(_options.Folder) ? _options.Folder : _settings.LastFolder;
        if (!string.IsNullOrWhiteSpace(folder))
            OpenFolder(folder!, revealInTree: true);
        Keyboard.Focus(FileList);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_isFullscreen) ExitFullscreen();
        foreach (var g in OpenGalleries.ToList()) g.CloseNow(); // stops their ffmpeg runs; no fade
        _uiTimer.Stop();
        ComponentDispatcher.ThreadPreprocessMessage -= OnThreadPreprocessMessage;
        _folderCts?.Cancel();
        _revealCts?.Cancel();

        CollectSettings();
        _player?.Dispose();
        _player = null;

        if (_iniWritable)
        {
            try
            {
                _settings.Save(_iniPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Settings could not be saved to {_iniPath}:\n\n{ex.Message}",
                    "viBro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private IEnumerable<KeyValuePair<string, string>> BuildMpvOptions()
    {
        var c = CultureInfo.InvariantCulture;
        // Isolation from any mpv installed on the machine, no subtitles, no mpv UI or key bindings.
        var o = new List<KeyValuePair<string, string>>
        {
            new("config", "no"),
            new("load-scripts", "no"),
            new("ytdl", "no"),
            new("terminal", "no"),
            new("osc", "no"),
            // OSD text (show-text: time after a seek and file name in fullscreen): smaller than mpv's default.
            new("osd-font-size", "36"),
            new("osd-border-size", "2"),
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
            // Hardware decoding where available, automatic software fallback (viBro.ini).
            new("hwdec", _settings.HardwareDecoding),
            new("volume", _settings.Volume.ToString("0.#", c)),
            new("mute", _settings.Mute ? "yes" : "no"),
        };

        // Cache: off by default (no large read-ahead on network shares); optional limit in viBro.ini.
        if (_settings.CacheMaxMB > 0)
        {
            o.Add(new("cache", "yes"));
            o.Add(new("demuxer-max-bytes", _settings.CacheMaxMB.ToString(c) + "MiB"));
        }
        else
        {
            o.Add(new("cache", "no"));
        }

        o.AddRange(_options.MpvOptions); // command-line overrides win (applied last)
        return o;
    }

    // ================================================================ settings <-> window

    private void ApplyWindowSettings()
    {
        var s = _settings;
        Width = s.WindowWidth;
        Height = s.WindowHeight;
        if (s.WindowLeft is double left && s.WindowTop is double top && IsOnScreen(left, top, Width, Height))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        if (s.WindowMaximized) WindowState = WindowState.Maximized;

        TreeColumn.Width = new GridLength(Math.Max(TreeColumn.MinWidth, s.TreeWidth));
        ListRow.Height = new GridLength(Math.Max(ListRow.MinHeight, s.ListHeight));
        NameColumn.Width = s.NameColumnWidth;
        SizeColumn.Width = s.SizeColumnWidth;
        DateColumn.Width = s.DateColumnWidth;
        _updatingVolume = true;
        VolumeSlider.Value = s.Volume;
        _updatingVolume = false;
    }

    private bool _updatingVolume;

    /// <summary>True when at least a usable part of the window would be visible on the desktop.</summary>
    internal static bool IsOnScreen(double left, double top, double width, double height)
    {
        var desktop = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        var r = Rect.Intersect(desktop, new Rect(left, top, width, height));
        return !r.IsEmpty && r.Width >= 150 && r.Height >= 80;
    }

    private void CollectSettings()
    {
        var s = _settings;
        Rect b = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
        if (!b.IsEmpty && b.Width > 0 && b.Height > 0)
        {
            s.WindowLeft = b.Left;
            s.WindowTop = b.Top;
            s.WindowWidth = b.Width;
            s.WindowHeight = b.Height;
        }
        s.WindowMaximized = WindowState == WindowState.Maximized;

        if (TreeColumn.ActualWidth > 0) s.TreeWidth = TreeColumn.ActualWidth;
        if (ListRow.ActualHeight > 0) s.ListHeight = ListRow.ActualHeight;
        if (!double.IsNaN(NameColumn.Width)) s.NameColumnWidth = NameColumn.Width;
        if (!double.IsNaN(SizeColumn.Width)) s.SizeColumnWidth = SizeColumn.Width;
        if (!double.IsNaN(DateColumn.Width)) s.DateColumnWidth = DateColumn.Width;
        s.SortColumn = _sortColumn;
        s.SortDescending = _sortDescending;

        if (_currentFolder != null) s.LastFolder = _currentFolder;
        s.Volume = VolumeSlider.Value;
        if (_player != null) s.Mute = _player.IsMuted;
    }

    // ================================================================ folder

    private async void OpenFolder(string rawPath, bool revealInTree, string? selectAfterOpen = null, Action? onOpened = null)
    {
        if (string.IsNullOrWhiteSpace(rawPath)) return;
        string path;
        try
        {
            path = PathUtil.NormalizeFolder(rawPath); // string-only, no I/O
        }
        catch (Exception ex)
        {
            SetStatus("Invalid path: " + ex.Message, StatusKind.Error);
            return;
        }

        int generation = ++_folderGeneration;
        _folderCts?.Cancel();
        var cts = new CancellationTokenSource();
        _folderCts = cts;

        PathBox.Text = path;
        UpdateBreadcrumb(path);
        SetStatus("Opening " + path + " ...");

        var scan = Task.Run(() => FolderScanner.Scan(path, cts.Token));

        // Tell the user when a share is slow to answer; the UI stays usable meanwhile.
        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            if (!scan.IsCompleted && generation == _folderGeneration)
                SetStatus("Still waiting for " + path + " (network location not responding?)", StatusKind.Error);
        }, TaskScheduler.FromCurrentSynchronizationContext());

        try
        {
            var items = await scan;
            if (generation != _folderGeneration) return; // superseded by a newer request

            _currentFolder = path;
            SetItems(items, keepSelection: false);
            int videos = 0, folders = 0;
            foreach (var it in items)
            {
                if (it.IsVideo) videos++;
                else if (it.Kind == EntryKind.Folder) folders++;
            }
            SetStatus($"{videos} video file(s), {folders} folder(s) in {path}");
            if (selectAfterOpen != null)
            {
                // Back with "..": select (not open) the folder just left, without loading anything.
                var back = items.Find(i => !i.IsVideo && string.Equals(Path.TrimEndingDirectorySeparator(i.FullPath),
                    Path.TrimEndingDirectorySeparator(selectAfterOpen), StringComparison.OrdinalIgnoreCase));
                if (back != null)
                {
                    FileList.SelectedItem = back;
                    FileList.ScrollIntoView(back);
                }
            }
            if (revealInTree) RevealInTree(path);
            onOpened?.Invoke(); // e.g. select the video chosen in the gallery
        }
        catch (OperationCanceledException)
        {
            // superseded
        }
        catch (Exception ex)
        {
            if (generation == _folderGeneration)
                SetStatus("Cannot open folder: " + ex.Message, StatusKind.Error);
        }
    }

    private void PathBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            string typed = PathBox.Text;
            LeavePathEdit();
            OpenFolder(typed, revealInTree: true);
            Keyboard.Focus(FileList);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            PathBox.Text = _currentFolder ?? "";
            LeavePathEdit();
            Keyboard.Focus(FileList);
            e.Handled = true;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Select a folder with videos" };
        if (!string.IsNullOrEmpty(_currentFolder)) dialog.InitialDirectory = _currentFolder;
        if (dialog.ShowDialog(this) == true)
            OpenFolder(dialog.FolderName, revealInTree: true);
    }

    private void FocusPathBox() => EnterPathEdit(); // Ctrl+L: the breadcrumbs become the editable path

    // ================================================================ folder tree

    private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not FolderNode node || node.IsPlaceholder) return;
        _treeSelected = node;
        if (ReferenceEquals(node, _revealedNode)) return; // selection made by RevealInTree
        _revealedNode = null;
        if (_currentFolder != null && string.Equals(node.Path, _currentFolder, StringComparison.OrdinalIgnoreCase))
            return;
        OpenFolder(node.Path, revealInTree: false);
    }

    private void FolderTree_ItemSelected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem item) item.BringIntoView();
    }

    /// <summary>Expands the tree down to the folder in the background, then selects it.</summary>
    private async void RevealInTree(string folder)
    {
        _revealCts?.Cancel();
        var cts = new CancellationTokenSource();
        _revealCts = cts;

        FolderNode? node;
        try
        {
            node = await _tree.RevealAsync(folder, cts.Token);
        }
        catch
        {
            return;
        }
        if (node == null || cts.IsCancellationRequested) return;

        if (_treeSelected != null && !ReferenceEquals(_treeSelected, node))
            _treeSelected.IsSelected = false; // avoid a stale selection on a virtualized item
        _revealedNode = node;
        node.IsSelected = true;
    }

    // ================================================================ file list

    private void SetItems(List<VideoItem> items, bool keepSelection)
    {
        var selected = keepSelection ? FileList.SelectedItems.Cast<object>().ToList() : new List<object>();
        items.Sort(CompareItems);
        if (selected.Count > 0)
        {
            var present = new HashSet<VideoItem>(items);
            selected.RemoveAll(o => o is not VideoItem v || !present.Contains(v)); // rows gone after F5
        }
        _suppressSelectionLoad = true;
        try
        {
            _items = items;
            FileList.ItemsSource = _items;
            foreach (var s in selected) FileList.SelectedItems.Add(s); // the whole selection (1.7.0)
            var visible = _currentItem != null && selected.Contains(_currentItem) ? _currentItem : selected.FirstOrDefault();
            if (visible != null) FileList.ScrollIntoView(visible);
        }
        finally
        {
            _suppressSelectionLoad = false;
        }
        UpdateFolderSummary();
    }

    private int CompareItems(VideoItem a, VideoItem b)
    {
        // ".." first, then folders, then videos, whatever the sort column or direction.
        if (a.Kind != b.Kind) return a.Kind.CompareTo(b.Kind);
        int c = _sortColumn switch
        {
            SortColumn.Size => a.Size.CompareTo(b.Size),
            SortColumn.Date => a.Modified.CompareTo(b.Modified),
            _ => 0,
        };
        if (c == 0) c = NaturalComparer.Instance.Compare(a.Name, b.Name);
        return _sortDescending ? -c : c;
    }

    private void FileList_HeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header || header.Column == null ||
            header.Role == GridViewColumnHeaderRole.Padding)
            return;

        var column = header.Column == SizeColumn ? SortColumn.Size
                   : header.Column == DateColumn ? SortColumn.Date
                   : SortColumn.Name;
        if (column == _sortColumn) _sortDescending = !_sortDescending;
        else
        {
            _sortColumn = column;
            _sortDescending = false;
        }
        UpdateSortHeaders();
        SetItems(new List<VideoItem>(_items), keepSelection: true);
    }

    private void UpdateSortHeaders()
    {
        string arrow = _sortDescending ? " ▼" : " ▲";
        NameColumn.Header = "Name" + (_sortColumn == SortColumn.Name ? arrow : "");
        SizeColumn.Header = "Size" + (_sortColumn == SortColumn.Size ? arrow : "");
        DateColumn.Header = "Date modified" + (_sortColumn == SortColumn.Date ? arrow : "");
    }

    /// <summary>
    /// Several rows can be selected (1.7.0, Ctrl / Shift + click, Ctrl+A). The video loaded is the one
    /// just clicked, if it is a selected video (a Ctrl+click that deselects a row loads nothing); for
    /// a selection changed without the mouse, the last video added. Folders load nothing.
    /// </summary>
    private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var clicked = _clickedItem;
        _clickedItem = null; // handled here
        // Changes made by code (bulk selections included) update the summary once, when they are done.
        if (_suppressSelectionLoad) return;
        UpdateFolderSummary(); // "12 / 128 videos" or "3 selected (4.2 GB) · 128 videos"
        VideoItem? target;
        if (clicked != null)
            target = clicked.IsVideo && FileList.SelectedItems.Contains(clicked) ? clicked : null;
        else
            target = e.AddedItems.OfType<VideoItem>().LastOrDefault(v => v.IsVideo);
        if (target != null && !ReferenceEquals(target, _currentItem) && !ReferenceEquals(target, _loadingItem))
            LoadItem(target);
    }

    private VideoItem? _clickedItem; // row under the left button, until the selection change it causes

    private void FileList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _clickedItem = ItemsControl.ContainerFromElement(FileList, e.OriginalSource as DependencyObject) is ListViewItem { Content: VideoItem v }
            ? v : null;
    }

    /// <summary>A click that did not change the selection (e.g. on a row already selected) still loads that video.</summary>
    private void FileList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var clicked = _clickedItem;
        _clickedItem = null;
        if (clicked is not { IsVideo: true } || Keyboard.Modifiers != ModifierKeys.None) return;
        if (!FileList.SelectedItems.Contains(clicked)) return;
        if (!ReferenceEquals(clicked, _currentItem) && !ReferenceEquals(clicked, _loadingItem)) LoadItem(clicked);
    }

    /// <summary>Ctrl+A: every video of the folder (not the folders); the loaded video does not change.</summary>
    private void SelectAllVideos()
    {
        _suppressSelectionLoad = true;
        try
        {
            FileList.SelectAll(); // one change for the whole list, then the folders (few) are removed
            foreach (var v in _items) if (!v.IsVideo) FileList.SelectedItems.Remove(v);
        }
        finally
        {
            _suppressSelectionLoad = false;
        }
        UpdateFolderSummary();
    }

    /// <summary>
    /// Loads a file keeping the play/pause state: if the current video is playing, the new one
    /// starts playing; if it is paused (or nothing was playing), the new one is loaded paused.
    /// After a file operation the state recorded before the player was released is used.
    /// </summary>
    private void LoadItem(VideoItem item)
    {
        if (_player == null) return;
        if (_busyPaths.Contains(item.FullPath))
        {
            SetStatus($"{item.Name} is being deleted or moved: it can be loaded when the operation ends.", StatusKind.Error);
            return;
        }
        bool play = _playAfterRelease ?? !_player.IsPaused;
        _playAfterRelease = null;
        _loadingItem = item;
        _currentInfo = null;
        SetStatus("Loading " + item.Name + " ...");
        UpdateVideoPlaceholder();
        _player.Load(item.FullPath, item, play);
    }

    /// <summary>
    /// "3 folders · 12 / 128 videos · 412.5 GB": subfolders, position of the selected video among the
    /// videos, number of videos and their total size.
    /// </summary>
    private void UpdateFolderSummary()
    {
        long total = 0, selectedBytes = 0;
        int videos = 0, folders = 0, position = 0, selectedCount = 0;
        var selection = new HashSet<VideoItem>(FileList.SelectedItems.OfType<VideoItem>());
        foreach (var v in _items)
        {
            if (v.Kind == EntryKind.Folder) folders++;
            if (!v.IsVideo) continue;
            videos++;
            total += v.Size;
            if (selection.Contains(v))
            {
                selectedCount++;
                selectedBytes += v.Size;
                position = videos;
            }
        }
        string count = videos == 1 ? "1 video" : $"{videos} videos";
        if (selectedCount == 1) count = $"{position} / {count}";
        else if (selectedCount > 1) count = $"{selectedCount} selected ({VideoItem.FormatSize(selectedBytes)}) · {count}";
        string folderText = folders == 0 ? "" : folders == 1 ? "1 folder · " : $"{folders} folders · ";
        FolderSummaryText.Text = folderText + count + " · " + VideoItem.FormatSize(total);
    }

    /// <summary>E / D and the wheel: previous / next video in the current order (folders are skipped), keeping play/pause. No wrap-around.</summary>
    private void SelectRelative(int direction)
    {
        if (_items.Count == 0) return;
        // From the loaded video (with several rows selected, the selection collapses to the new one).
        var from = _loadingItem ?? _currentItem;
        int index = from != null ? _items.IndexOf(from) : -1;
        if (index < 0) index = FileList.SelectedIndex;
        int next = index < 0 ? (direction > 0 ? 0 : _items.Count - 1) : index + direction;
        while (next >= 0 && next < _items.Count && !_items[next].IsVideo) next += direction;
        if (next < 0 || next >= _items.Count) return;
        SelectAndLoad(_items[next]);
    }

    /// <summary>Double click on a folder or "..": opens it (videos: nothing, the single click already loaded them).</summary>
    private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (ItemsControl.ContainerFromElement(FileList, e.OriginalSource as DependencyObject) is not ListViewItem row) return;
        if (row.Content is VideoItem { IsVideo: false } entry)
        {
            OpenListEntry(entry);
            e.Handled = true;
        }
    }

    /// <summary>Opens a folder of the list; going up with "..", the folder just left is selected.</summary>
    private void OpenListEntry(VideoItem entry)
    {
        string? cameFrom = entry.Kind == EntryKind.Parent ? _currentFolder : null;
        OpenFolder(entry.FullPath, revealInTree: true, selectAfterOpen: cameFrom);
        Keyboard.Focus(FileList);
    }

    /// <summary>Shift+Enter: opens the selected video with the Windows default application; viBro pauses.</summary>
    private void OpenWithDefaultApp()
    {
        if (RejectMultipleSelection("Shift+Enter") || SelectedVideo is not VideoItem item) return;
        if (_player != null && !_player.IsPaused) _player.TogglePause(); // no two audio tracks at once
        string path = item.FullPath;
        SetStatus("Opening " + item.Name + " with the default application ...");
        Task.Run(() =>
        {
            try
            {
                using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                Dispatcher.BeginInvoke(() => SetStatus("Opened " + item.Name + " with the default application."));
            }
            catch (Exception ex)
            {
                ErrorLog.Write("shell", "Cannot open " + item.Name + " with the default application", ex);
                Dispatcher.BeginInvoke(() => SetStatus("Cannot open " + item.Name + " with the default application: " + ex.Message, StatusKind.Error));
            }
        });
    }

    private void OnLoadCompleted(LoadResult r)
    {
        if (r.Tag is not VideoItem item || !ReferenceEquals(item, _loadingItem)) return;
        _currentInfo = r.Info;
        SetCurrentItem(item);
        if (_isFullscreen) _player?.ShowText(item.Name, FileNameOsdMs); // which file, without the list
        // 1.13.0: the format on the right of the status bar; engine details in its tooltip.
        string codec = string.IsNullOrEmpty(r.Info.Codec) ? "" : r.Info.Codec.ToUpperInvariant();
        MediaText.Text = r.Info.Width > 0 ? $"{codec} {r.Info.Width}×{r.Info.Height}".Trim() : "no video";
        MediaText.ToolTip = $"{item.Name}\nhwdec: {Display(r.Info.Hwdec)}\n{r.Kind} {r.ElapsedMs:0} ms";
        ApplyPostLoadActions(item); // position after a reload, result of the last file operation
        _lastLoadText = $"{r.Kind} {r.ElapsedMs:0} ms  |  hwdec: {Display(r.Info.Hwdec)}";
        UpdateMetricsText();

        RecordMetric(r.Kind, r.ElapsedMs, item, seekMode: "", delta: null, target: null, landed: null, coalesced: 0);
    }

    private void OnLoadFailed(string path, string error)
    {
        SetCurrentItem(null);
        SetStatus($"Cannot play {Path.GetFileName(path)}: {error}", StatusKind.Error);
        ErrorLog.Write("playback", $"Cannot open {Path.GetFileName(path)}: {error}");
    }

    // ================================================================ seeking and transport

    private void Seek(double deltaSeconds)
    {
        ShowFullscreenBar();
        _player?.SeekRelative(deltaSeconds, _exactLongSeeks || Math.Abs(deltaSeconds) < _exactThreshold);
    }

    private void OnSeekCompleted(SeekResult r)
    {
        if (r.Tag is not VideoItem item) return;
        string kind = r.TimedOut ? "seek_timeout" : "seek";
        if (!r.TimedOut) _seekStats.Add(r.ElapsedMs);
        _lastSeekText = $"seek {r.ElapsedMs:0} ms (median {_seekStats.Median:0}, n={_seekStats.Count})";
        UpdateMetricsText();

        RecordMetric(kind, r.ElapsedMs, item, r.Exact ? "exact" : "keyframes", r.Delta, r.Target, r.Landed, r.Coalesced);
        if (!r.TimedOut) ShowTimeOsd(r.Landed ?? r.Target); // fullscreen only
    }

    private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingTimeSlider || _player == null) return;
        ShowFullscreenBar();
        _player.SeekAbsolute(e.NewValue, _exactLongSeeks); // coalesced while dragging
    }

    private void RefreshTransport()
    {
        if (_player == null) return;
        double duration = _player.Duration;
        double position = Math.Min(_player.TimePos, Math.Max(duration, 0));

        TimeText.Text = FormatTime(position) + " / " + FormatTime(duration);
        UpdateTransportIcons(_player.IsPaused, _player.IsMuted);
        _currentItem?.SetCurrent(true, _player.IsPaused);

        if (!TimeSlider.IsMouseCaptureWithin && !_scrubbing) // the user is choosing the position
        {
            _updatingTimeSlider = true;
            TimeSlider.Maximum = duration > 0 ? duration : 1;
            TimeSlider.Value = Math.Clamp(position, 0, TimeSlider.Maximum);
            _updatingTimeSlider = false;
        }
        UpdateMarkerVisuals(); // I / O on the timeline (moves only when something changed)
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e) => _player?.TogglePause();

    private void MuteButton_Click(object sender, RoutedEventArgs e) => _player?.ToggleMute();

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingVolume) return;
        _player?.SetVolume(e.NewValue);
    }

    // ================================================================ keyboard

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Close (Ctrl+W by default) works even while a text field is being edited.
        Key pressed = e.Key == Key.System ? e.SystemKey : e.Key;
        if (IsActive && _settings.Keys.TryGet(pressed, Keyboard.Modifiers, out Command c) && c == Command.Close)
        {
            e.Handled = true;
            Dispatcher.BeginInvoke(new Action(Close));
            return;
        }
        // Other shortcuts are suspended while a text field is being edited.
        if (Keyboard.FocusedElement is TextBoxBase) return;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        // Not e.IsRepeat: WPF calls a key "repeated" when it sees the same key down twice without a
        // key-up in between, and the key-up of R/Del/F2/W is lost to the confirmation dialog they
        // open. The next press of the same key was then ignored (it had to be pressed twice).
        if (HandleShortcut(key, Keyboard.Modifiers, _win32KeyRepeat))
            e.Handled = true;
    }

    private void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        bool isKey = msg.message == NativeMethods.WM_KEYDOWN || msg.message == NativeMethods.WM_SYSKEYDOWN;
        // Windows' own auto-repeat flag (bit 30: the key was already down), recorded for every key
        // message of this thread before WPF turns it into KeyDown (read by Window_PreviewKeyDown).
        if (isKey) _win32KeyRepeat = (msg.lParam.ToInt64() & 0x40000000) != 0;
        if (_options.TraceInput && (isKey || msg.message == NativeMethods.WM_MOUSEWHEEL)) TraceInputMessage(msg);
        if (handled) return;

        // The wheel goes to the focused window or to the one under the pointer (Windows setting).
        // Over the time bar it seeks (both modes); elsewhere in fullscreen it changes video.
        if (msg.message == NativeMethods.WM_MOUSEWHEEL)
        {
            if (IsOwnWindowMessage(msg.hwnd) && OnWheel(NativeMethods.WheelDelta(msg.wParam), msg.pt_x, msg.pt_y))
                handled = true;
            return;
        }

        bool isMouse = msg.message is NativeMethods.WM_MOUSEMOVE or NativeMethods.WM_LBUTTONDOWN
                                   or NativeMethods.WM_LBUTTONDBLCLK or NativeMethods.WM_RBUTTONDOWN
                                   or NativeMethods.WM_RBUTTONUP;
        if (!isKey && !isMouse) return;

        IntPtr host = VideoHost.Handle;
        if (host == IntPtr.Zero) return;
        bool toVideo = msg.hwnd == host || NativeMethods.IsChild(host, msg.hwnd);

        if (isMouse)
        {
            // Normally mpv's window gets the mouse; if it ever let messages through to the main
            // window, the pointer position still tells whether the click was on the video.
            // During a right-button drag the main window holds the mouse capture: all its messages count.
            IntPtr main = new WindowInteropHelper(this).Handle;
            if (toVideo || (msg.hwnd == main && (_rightDown || IsPointOverVideo(msg.pt_x, msg.pt_y))))
                OnVideoMouseMessage(msg); // double click, right-click menu, fullscreen band
            return; // never mark mouse messages as handled
        }
        if (!toVideo) return;

        Key key = KeyInterop.KeyFromVirtualKey((int)msg.wParam.ToInt64());
        if (HandleShortcut(key, NativeMethods.CurrentModifiers(), _win32KeyRepeat))
            handled = true;
    }

    /// <summary>
    /// Keyboard shortcuts of the main window, from [Keys] in viBro.ini (1.10.0; modifiers must match
    /// exactly). Auto-repeat runs only seeks, previous / next video and other harmless commands; file
    /// operations and toggles run once per key press. Returns false for keys that do nothing here.
    /// </summary>
    private bool HandleShortcut(Key key, ModifierKeys mods, bool isRepeat)
    {
        if (!IsActive) return false; // e.g. a modal dialog is open
        if (!_settings.Keys.TryGet(key, mods, out Command command)) return false;

        // Commands that only apply in some states: otherwise the key keeps its normal meaning.
        VideoItem? folderEntry = FileList.SelectedItems.Count == 1 && FileList.SelectedItem is VideoItem { IsVideo: false } entry ? entry : null;
        if (command == Command.OpenFolder && folderEntry == null) return false;
        if (command == Command.ExitFullscreen && !_isFullscreen) return false;

        if (isRepeat && !KeyBindings.AllowsRepeat(command)) return true; // one key press = at most one operation

        switch (command)
        {
            // Steps from [Seek] in viBro.ini (1.4.0)
            case Command.SeekBackShort: Seek(-_settings.SeekLeftRightSeconds); break;
            case Command.SeekForwardShort: Seek(+_settings.SeekLeftRightSeconds); break;
            case Command.SeekBackMedium: Seek(-_settings.SeekUpDownSeconds); break;
            case Command.SeekForwardMedium: Seek(+_settings.SeekUpDownSeconds); break;
            case Command.SeekBackLong: Seek(-_settings.SeekPageSeconds); break;
            case Command.SeekForwardLong: Seek(+_settings.SeekPageSeconds); break;
            case Command.SeekBackVeryLong: Seek(-_settings.SeekAltPageSeconds); break;
            case Command.SeekForwardVeryLong: Seek(+_settings.SeekAltPageSeconds); break;

            case Command.PlayPause: _player?.TogglePause(); break;
            case Command.PreviousVideo: SelectRelative(-1); break;
            case Command.NextVideo: SelectRelative(+1); break;
            case Command.Mute: _player?.ToggleMute(); break;
            case Command.VolumeUp: ChangeVolume(+VolumeStep); break;
            case Command.VolumeDown: ChangeVolume(-VolumeStep); break;
            case Command.ParentFolder: OpenParentFolder(); break;
            case Command.Fullscreen: ToggleFullscreen(); break;
            case Command.ExitFullscreen: ExitFullscreen(); break;
            case Command.FocusPath: FocusPathBox(); break;
            case Command.SelectAll: SelectAllVideos(); break;
            case Command.Refresh: RefreshAll(); break;
            case Command.OpenFolder: OpenListEntry(folderEntry!); break;
            case Command.OpenWithDefaultApp: OpenWithDefaultApp(); break;

            case Command.Delete: DeleteSelected(fromVideoMenu: false); break;
            case Command.Rename: RenameSelected(); break;
            case Command.Move: MoveSelected(useLastDestination: false); break;
            case Command.MoveToLast: MoveSelected(useLastDestination: true); break;
            case Command.Copy: CopySelected(useLastDestination: false); break;
            case Command.CopyToLast: CopySelected(useLastDestination: true); break;

            // Extract (1.5.0): markers on the loaded file, then the part between them.
            case Command.MarkIn: SetMarker(start: true); break;
            case Command.MarkOut: SetMarker(start: false); break;
            case Command.Extract: ExtractMarked(); break;

            // F10 by default, a system key: handled here so Windows does not enter menu mode.
            case Command.Gallery: Dispatcher.BeginInvoke(new Action(OpenGallery)); break; // after this key message
            case Command.SelectionGallery: Dispatcher.BeginInvoke(new Action(OpenSelectionGallery)); break;
            case Command.Commands: Dispatcher.BeginInvoke(new Action(() => CommandsDialog.Open(this, _settings))); break;
            case Command.Close: Dispatcher.BeginInvoke(new Action(Close)); break;
        }
        return true;
    }

    // ================================================================ volume, cursor, placeholder (1.13.0)

    private const double VolumeStep = 5;

    /// <summary>VolumeUp / VolumeDown (+ / - by default): 5 % per press, the value shown over the video.</summary>
    internal void ChangeVolume(double delta)
    {
        if (_player == null) return;
        VolumeSlider.Value = Math.Clamp(Math.Round(VolumeSlider.Value + delta), 0, 100); // sets the player's volume
        _player.ShowText($"Volume {VolumeSlider.Value:0} %" + (_player.IsMuted ? " (muted)" : ""), 1000);
    }

    private bool _cursorHidden;

    /// <summary>Fullscreen: the pointer disappears with the band and comes back with the first movement.</summary>
    private void HideCursor()
    {
        if (_cursorHidden || !_isFullscreen || !IsActive) return;
        _cursorHidden = true;
        NativeMethods.ShowCursor(false); // also over the video window (it shares viBro's input)
        Mouse.OverrideCursor = Cursors.None;
    }

    private void RestoreCursor()
    {
        if (!_cursorHidden) return;
        _cursorHidden = false;
        NativeMethods.ShowCursor(true);
        Mouse.OverrideCursor = null;
    }

    /// <summary>
    /// While nothing is loaded the native video window is hidden and a guide text is shown instead
    /// (nothing can be drawn over the video window itself).
    /// </summary>
    private void UpdateVideoPlaceholder()
    {
        if (_player == null) return;
        bool idle = _currentItem == null && _loadingItem == null;
        if (idle)
            VideoPlaceholder.Text = $"Select a video  ·  {_settings.Keys.Display(Command.Gallery)} gallery  ·  " +
                                    $"{_settings.Keys.Display(Command.Commands)} commands";
        VideoPlaceholder.Visibility = idle ? Visibility.Visible : Visibility.Collapsed;
        VideoHost.Visibility = idle ? Visibility.Hidden : Visibility.Visible;
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
        _savedListHeight = ListRow.Height;
        _savedListMinHeight = ListRow.MinHeight;
        _savedTreeWidth = TreeColumn.Width;
        _savedTreeMinWidth = TreeColumn.MinWidth;

        SetChromeVisibility(Visibility.Collapsed);
        ListRow.MinHeight = 0;
        ListRow.Height = new GridLength(0);
        TreeColumn.MinWidth = 0;
        TreeColumn.Width = new GridLength(0);

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
        WindowState = WindowState.Maximized;
        SetTransportFullscreen(true);
    }

    private void ExitFullscreen()
    {
        _isFullscreen = false;
        RestoreCursor();
        WindowStyle = _savedStyle;
        ResizeMode = _savedResizeMode;
        WindowState = _savedState == WindowState.Minimized ? WindowState.Normal : _savedState;

        ListRow.Height = _savedListHeight;
        ListRow.MinHeight = _savedListMinHeight;
        TreeColumn.Width = _savedTreeWidth;
        TreeColumn.MinWidth = _savedTreeMinWidth;
        SetChromeVisibility(Visibility.Visible);
        SetTransportFullscreen(false);
    }

    // The transport bar is docked below the video (above the file list in the normal window).
    // Fullscreen band: the same bar as a black strip below the video (the picture is never covered)
    // holding only the timeline, centred and ~40% of the screen wide. It appears on mouse movement or
    // seeks and hides after 2 s without activity; the strip itself stays, so the video does not resize.
    private void SetTransportFullscreen(bool on)
    {
        _fullscreenBarTimer.Stop();
        if (on)
        {
            TransportBar.Background = System.Windows.Media.Brushes.Black;
            TimeText.Foreground = System.Windows.Media.Brushes.White;
            PlayButton.Visibility = Visibility.Collapsed;
            AudioControls.Visibility = Visibility.Collapsed;
            SeekPanel.HorizontalAlignment = HorizontalAlignment.Center;
            Dispatcher.InvokeAsync(SizeFullscreenBar, DispatcherPriority.Loaded);
            ShowFullscreenBar();
        }
        else
        {
            TransportBar.ClearValue(Border.BackgroundProperty);
            TimeText.ClearValue(TextBlock.ForegroundProperty);
            PlayButton.Visibility = Visibility.Visible;
            AudioControls.Visibility = Visibility.Visible;
            SeekPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            SeekPanel.Width = double.NaN;
            SeekPanel.Visibility = Visibility.Visible;
        }
    }

    private void SizeFullscreenBar()
    {
        if (_isFullscreen) SeekPanel.Width = Math.Max(320, ActualWidth * 0.4);
    }

    private void ShowFullscreenBar()
    {
        if (!_isFullscreen) return;
        RestoreCursor();
        SeekPanel.Visibility = Visibility.Visible;
        _fullscreenBarTimer.Stop();
        _fullscreenBarTimer.Start();
    }

    private void HideFullscreenBarIfIdle()
    {
        if (!_isFullscreen) { _fullscreenBarTimer.Stop(); return; }
        if (TransportBar.IsMouseOver || TimeSlider.IsMouseCaptureWithin) return; // keep while in use
        _fullscreenBarTimer.Stop();
        SeekPanel.Visibility = Visibility.Hidden; // Hidden keeps the band's height
        HideCursor(); // 1.13.0: with the band, after 2 s without mouse movement
    }

    private void TransportBar_MouseMove(object sender, MouseEventArgs e) => ShowFullscreenBar();

    // ================================================================ theme

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.Theme = ThemeManager.IsDark(_settings.Theme) ? ThemeChoice.Light : ThemeChoice.Dark;
        ThemeManager.Apply(_settings.Theme);
        ApplyItemStyles();
        UpdateThemeButton();
    }

    /// <summary>
    /// Item container styles built on top of the current theme's styles (a style without BasedOn
    /// would replace the Fluent look, e.g. black text in dark mode). Rebuilt on every theme change.
    /// </summary>
    private const double CompactRowHeight = 24;

    private void ApplyItemStyles()
    {
        // A ListView with a GridView needs the GridView row style (its template has a
        // GridViewRowPresenter that draws the columns). The plain ListViewItem style only has a
        // ContentPresenter and would show the item's ToString() ("ViBro.Util.VideoItem").
        var baseListStyle = TryFindResource(GridView.GridViewItemContainerStyleKey) as Style
                            ?? TryFindResource(typeof(ListViewItem)) as Style;
        var listItem = new Style(typeof(ListViewItem), baseListStyle);
        listItem.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        // Compact rows (~24 px). Fluent uses a 40 px minimum and a 2 px gap under each row.
        listItem.Setters.Add(new Setter(MinHeightProperty, CompactRowHeight));
        listItem.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
        FileList.ItemContainerStyle = listItem;

        var treeItem = new Style(typeof(TreeViewItem), TryFindResource(typeof(TreeViewItem)) as Style);
        // Same height as the file list: Fluent pads the header by 4 px and leaves a 2 px gap
        // (applied twice by its template, which reuses Margin on the inner border).
        treeItem.Setters.Add(new Setter(PaddingProperty, new Thickness(4, 2.5, 4, 2.5)));
        treeItem.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
        treeItem.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty,
            new System.Windows.Data.Binding(nameof(FolderNode.IsExpanded)) { Mode = System.Windows.Data.BindingMode.TwoWay }));
        treeItem.Setters.Add(new Setter(TreeViewItem.IsSelectedProperty,
            new System.Windows.Data.Binding(nameof(FolderNode.IsSelected)) { Mode = System.Windows.Data.BindingMode.TwoWay }));
        FolderTree.ItemContainerStyle = treeItem;
    }

    private void UpdateThemeButton()
    {
        bool dark = ThemeManager.IsDark(_settings.Theme);
        ThemeButton.Content = dark ? MoonGlyph : SunGlyph; // the current theme
        ThemeButton.ToolTip = dark ? "Theme: Dark (click for Light)" : "Theme: Light (click for Dark)";
    }

    // ================================================================ errors and startup

    private void OnPlaybackFailed(string error)
    {
        string name = _loadingItem?.Name ?? (FileList.SelectedItem as VideoItem)?.Name ?? "the video";
        SetStatus($"Playback of {name} stopped: {error} (network location lost?)", StatusKind.Error);
        ErrorLog.Write("playback", $"Playback of {name} stopped: {error}");
    }

    /// <summary>Time from process start to the first rendered window (used to compare EXE variants).</summary>
    private void RecordStartup()
    {
        try
        {
            using var proc = System.Diagnostics.Process.GetCurrentProcess();
            double ms = (DateTime.Now - proc.StartTime).TotalMilliseconds;
            long exeSize = Environment.ProcessPath != null ? new FileInfo(Environment.ProcessPath).Length : 0;
            _lastLoadText = $"startup {ms:0} ms";
            UpdateMetricsText();
            RecordMetric("startup", ms, null, "", null, null, null, 0, "viBro.exe", exeSize);
        }
        catch
        {
            // diagnostics only
        }
    }

    private void SetChromeVisibility(Visibility v)
    {
        TopBar.Visibility = v;
        StatusPanel.Visibility = v;
        ListSplitter.Visibility = v;
        FileList.Visibility = v;
        FolderTree.Visibility = v;
        TreeSplitter.Visibility = v;
    }

    // ================================================================ metrics and status

    private void RecordMetric(string kind, double ms, VideoItem? item, string seekMode,
        double? delta, double? target, double? landed, int coalesced, string? fileName = null, long fileSize = 0)
    {
        if (_metrics == null) return;
        var mem = MetricsLog.MemorySnapshot();
        var info = item != null ? _currentInfo : null;
        _metrics.Append(new MetricRecord(
            DateTime.UtcNow, kind, ms, item?.Name ?? fileName ?? "", item?.Size ?? fileSize, item?.Storage ?? "",
            seekMode, delta, target, landed, coalesced,
            info?.Codec, info?.Width ?? 0, info?.Height ?? 0, info?.Hwdec, info?.Vo,
            mem.WorkingSetMb, mem.PrivateMb, mem.ManagedMb, AppInfo.SemanticVersion));

        if (_metrics.LastError != null)
            SetStatus("Metrics not saved: " + _metrics.LastError, StatusKind.Error);
    }

    /// <summary>Load / seek times, hwdec and memory: only with [Diagnostics] MetricsLog=true (1.13.0).</summary>
    private void UpdateMetricsText()
    {
        var visibility = _settings.MetricsLog ? Visibility.Visible : Visibility.Collapsed;
        MetricsItem.Visibility = visibility;
        MetricsSeparator.Visibility = visibility;
        if (!_settings.MetricsLog) return;
        var mem = MetricsLog.MemorySnapshot();
        MetricsText.Text = string.Join("  |  ",
            new[] { _lastLoadText, _lastSeekText, $"mem {mem.PrivateMb:0} MB" }.Where(s => s.Length > 0));
    }

    private void ShowWarnings()
    {
        foreach (var w in _warnings.Skip(_loggedWarnings)) ErrorLog.Write("warning", w);
        _loggedWarnings = _warnings.Count;
        if (_warnings.Count == 0)
        {
            WarningText.Visibility = Visibility.Collapsed;
            return;
        }
        WarningText.Text = _warnings.Count == 1 ? "⚠ " + _warnings[0] : $"⚠ {_warnings.Count} warnings (hover for details)";
        WarningText.ToolTip = string.Join(Environment.NewLine, _warnings);
        WarningText.Visibility = Visibility.Visible;
    }


    private static string Display(string? s) => string.IsNullOrEmpty(s) ? "-" : s;

    /// <summary>1.13.0: one format everywhere, without useless leading zeros: "1:20", "15:02", "1:02:15".</summary>
    private static string FormatTime(double seconds) => TimeFormat.Format(seconds);
}
