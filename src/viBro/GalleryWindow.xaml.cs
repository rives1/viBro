using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ViBro.Util;

namespace ViBro;

/// <summary>
/// Gallery of the videos of one folder (F10, 1.6.0). The images are made in the background by a few
/// ffmpeg runs at a time, starting from the rows on screen; closing the window stops everything.
/// The folder is the one open when F10 was pressed: the gallery does not follow later folder changes.
/// </summary>
public partial class GalleryWindow : Window
{
    private readonly MainWindow _main;
    private readonly double[] _percents;
    private readonly int _jobs;
    private readonly List<GalleryRow> _rows; // work list (guarded by _gate)
    private readonly ObservableCollection<GalleryRow> _view; // what the window shows
    private readonly CancellationTokenSource _cts = new();
    private readonly object _gate = new();
    private readonly Stopwatch _watch = Stopwatch.StartNew();
    private volatile int _priorityIndex; // first row on screen: work starts there
    private int _running;
    private bool _suppressSelection;
    private ScrollViewer? _scroller;
    private VirtualizingStackPanel? _panel;

    /// <param name="compact">F12 (1.11.0): one compact strip per selected video, name only in the tooltip, no times.</param>
    internal GalleryWindow(MainWindow main, string title, IReadOnlyList<VideoItem> videos, double[] percents, int jobs, bool compact = false)
    {
        InitializeComponent();
        ThemeManager.AttachWindow(this);
        _main = main;
        _percents = percents;
        _jobs = Math.Max(1, jobs);
        _rows = videos.Select((v, i) => new GalleryRow(v, i, percents.Length)).ToList();
        _view = new ObservableCollection<GalleryRow>(_rows);
        Title = title;
        Rows.ItemTemplate = (DataTemplate)FindResource(compact ? "CompactRow" : "NormalRow");
        if (compact)
        {
            // Strips as close as possible: no padding or margin around the rows (based on the theme's style if any).
            var style = new Style(typeof(ListBoxItem), TryFindResource(typeof(ListBoxItem)) as Style);
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
            style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 1)));
            style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 0.0));
            Rows.ItemContainerStyle = style;
        }
        Rows.ItemsSource = _view;
        UpdateStatus();
        Loaded += (_, _) => Start();
    }

    private void Start()
    {
        _scroller = FindChild<ScrollViewer>(Rows);
        for (int i = 0; i < _jobs; i++)
        {
            Interlocked.Increment(ref _running);
            _ = Task.Run(() => WorkerAsync(_cts.Token));
        }
    }

    // ------------------------------------------------------------------ work

    private GalleryRow? NextRow()
    {
        lock (_gate)
        {
            int start = Math.Clamp(_priorityIndex, 0, Math.Max(0, _rows.Count - 1));
            for (int k = 0; k < _rows.Count; k++)
            {
                var row = _rows[(start + k) % _rows.Count]; // from the first row on screen, then the rest
                if (row.State != RowState.Pending || row.Removed) continue;
                row.State = RowState.Running;
                return row;
            }
            return null;
        }
    }

    private async Task WorkerAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && NextRow() is GalleryRow row)
            {
                bool ok = await MakeRowAsync(row, ct);
                row.Failed = !ok && !row.Removed; // a file deleted or moved meanwhile is not an error
                row.State = RowState.Done;
                if (ct.IsCancellationRequested) return;
                Ui(UpdateStatus);
            }
        }
        catch (OperationCanceledException)
        {
            // window closed
        }
        catch (Exception ex)
        {
            ErrorLog.Write("gallery", "Gallery worker stopped", ex);
        }
        finally
        {
            if (Interlocked.Decrement(ref _running) == 0) Ui(UpdateStatus);
        }
    }

    /// <summary>Probe, then one image per position. False if the duration or any image could not be read.</summary>
    private async Task<bool> MakeRowAsync(GalleryRow row, CancellationToken ct)
    {
        string key = GalleryCache.Key(row.Item, _percents);
        if (GalleryCache.TryGet(key, out var cached))
        {
            Ui(() => row.Apply(cached));
            return true;
        }

        ProbeResult probe;
        try
        {
            probe = await ThumbnailExtractor.ProbeAsync(row.Item.FullPath, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Ui(() => row.SetError("Cannot read: " + ex.Message));
            return false;
        }
        Ui(() => row.Info = FormatTime(probe.Duration));

        double[] times = ThumbnailExtractor.Positions(probe.Duration, _percents);
        var images = new Thumbnail?[times.Length];
        bool all = true;
        for (int i = 0; i < times.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            int index = i;
            try
            {
                var t = await ThumbnailExtractor.GrabAsync(row.Item.FullPath, times[i], probe.Start, ct);
                images[i] = t;
                Ui(() => row.Cells[index].SetImage(t));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                all = false;
                Ui(() => row.Cells[index].SetError());
                if (i == 0) ErrorLog.Write("gallery", $"No image from {row.Item.Name} at {times[i]:0.0} s: {ex.Message}");
            }
        }
        if (all) GalleryCache.Put(key, new GalleryEntry(probe.Duration, images.Select(t => t!).ToArray()));
        return all;
    }

    private void Ui(Action a)
    {
        if (_cts.IsCancellationRequested) return;
        Dispatcher.InvokeAsync(() =>
        {
            if (!_cts.IsCancellationRequested) a();
        });
    }

    private void UpdateStatus()
    {
        int total = _view.Count, done = _view.Count(r => r.State == RowState.Done);
        int failed = _view.Count(r => r.Failed);
        string errors = failed > 0 ? $"  ·  {failed} with errors (see viBro-errors.log)" : "";
        StatusText.Text = done < total && _running > 0
            ? $"{total} videos  ·  images {done} / {total} ...{errors}  ·  click: load  ·  right click: delete, move, copy"
            : $"{total} videos  ·  done in {_watch.Elapsed.TotalSeconds:0} s{errors}  ·  click: load  ·  right click: delete, move, copy";
    }

    // ------------------------------------------------------------------ input

    /// <summary>The first row on screen gets its images first (also after scrolling).</summary>
    private void Rows_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _panel ??= FindChild<VirtualizingStackPanel>(Rows);
        if (_panel == null || _scroller == null) return;
        int first = int.MaxValue;
        foreach (UIElement child in _panel.Children)
        {
            if (child is not FrameworkElement fe) continue;
            int index = Rows.ItemContainerGenerator.IndexFromContainer(fe);
            if (index < 0) continue;
            double bottom = fe.TranslatePoint(new Point(0, fe.ActualHeight), _scroller).Y;
            if (bottom > 0 && index < first) first = index;
        }
        if (first != int.MaxValue) _priorityIndex = first;
    }

    private void Rows_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelection) return;
        if (Rows.SelectedItem is GalleryRow row) _main.ShowFromGallery(row.Item.FullPath, null);
    }

    /// <summary>A click on the selected row (no selection change) loads it again if another file is loaded.</summary>
    private void Rows_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(Rows, e.OriginalSource as DependencyObject) is ListBoxItem { DataContext: GalleryRow row }
            && ReferenceEquals(Rows.SelectedItem, row))
            _main.ShowFromGallery(row.Item.FullPath, null);
    }

    private void Cell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: GalleryCell cell }) return;
        e.Handled = true; // the row is selected below, without a second load
        _suppressSelection = true;
        try
        {
            Rows.SelectedItem = cell.Row;
        }
        finally
        {
            _suppressSelection = false;
        }
        (Rows.ItemContainerGenerator.ContainerFromItem(cell.Row) as ListBoxItem)?.Focus();
        _main.ShowFromGallery(cell.Row.Item.FullPath, cell.HasImage ? cell.Seconds : null);
    }

    /// <summary>
    /// Right click on a row or an image (1.8.0): selects and loads that video in the main window, like
    /// a left click on the row, then opens the same menu as on the video (Delete, Move, Copy).
    /// </summary>
    private void Rows_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(Rows, e.OriginalSource as DependencyObject) is not ListBoxItem { DataContext: GalleryRow row } container)
            return;
        e.Handled = true;
        _suppressSelection = true;
        try
        {
            Rows.SelectedItem = row;
        }
        finally
        {
            _suppressSelection = false;
        }
        container.Focus();
        _main.ShowFromGallery(row.Item.FullPath, null);
        _main.ShowGalleryFileMenu(row.Item.FullPath, container);
    }

    /// <summary>Rows of files deleted or moved by viBro (or gone at F5) leave the gallery (1.8.0).</summary>
    internal void RemoveVideos(IEnumerable<string> paths)
    {
        var set = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
        if (set.Count == 0) return;
        foreach (var row in _view.Where(r => set.Contains(r.Item.FullPath)).ToList())
        {
            lock (_gate) row.Removed = true; // a pending row is no longer picked; a running one finishes unseen
            _view.Remove(row);
        }
        UpdateStatus();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    private void Window_Closed(object? sender, EventArgs e) => _cts.Cancel(); // running ffmpeg processes are killed

    // ------------------------------------------------------------------ helpers

    internal static string FormatTime(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, Math.Floor(seconds)));
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes}:{t.Seconds:00}";
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            if (FindChild<T>(child) is T deeper) return deeper;
        }
        return null;
    }
}

internal enum RowState { Pending, Running, Done }

/// <summary>One video of the gallery.</summary>
internal sealed class GalleryRow : INotifyPropertyChanged
{
    private string _info = "";

    public GalleryRow(VideoItem item, int index, int cells)
    {
        Item = item;
        Index = index;
        Cells = Enumerable.Range(0, cells).Select(i => new GalleryCell(this, i)).ToArray();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public VideoItem Item { get; }
    public int Index { get; }
    public string Name => Item.Name;
    public GalleryCell[] Cells { get; }
    public RowState State { get; set; } // guarded by the window's lock
    public bool Removed { get; set; }   // file deleted or moved: row taken out of the window
    public bool Failed { get; set; }    // duration or an image could not be read

    /// <summary>Duration, or the reason the file could not be read.</summary>
    public string Info
    {
        get => _info;
        set
        {
            _info = value;
            PropertyChanged?.Invoke(this, new(nameof(Info)));
        }
    }

    public void Apply(GalleryEntry entry)
    {
        Info = GalleryWindow.FormatTime(entry.Duration);
        for (int i = 0; i < Cells.Length && i < entry.Images.Length; i++) Cells[i].SetImage(entry.Images[i]);
    }

    public void SetError(string message)
    {
        Info = message;
        foreach (var c in Cells) c.SetError();
    }
}

/// <summary>
/// One image. Only the JPEG bytes (~10-30 KB) are kept; the bitmap is decoded when a row is on
/// screen, so a large folder does not hold hundreds of decoded images in memory.
/// </summary>
internal sealed class GalleryCell : INotifyPropertyChanged
{
    private byte[]? _jpeg;
    private string _label = "...";

    public GalleryCell(GalleryRow row, int index)
    {
        Row = row;
        Index = index;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public GalleryRow Row { get; }
    public int Index { get; }
    public double Seconds { get; private set; }
    public bool HasImage => _jpeg != null;
    public string Label => _label;

    public ImageSource? Image
    {
        get
        {
            if (_jpeg == null) return null;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = new MemoryStream(_jpeg, writable: false);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch (Exception)
            {
                return null; // a broken image stays an empty box
            }
        }
    }

    public void SetImage(Thumbnail t)
    {
        _jpeg = t.Jpeg;
        Seconds = t.Seconds;
        _label = GalleryWindow.FormatTime(t.Seconds);
        PropertyChanged?.Invoke(this, new(nameof(Image)));
        PropertyChanged?.Invoke(this, new(nameof(Label)));
    }

    public void SetError()
    {
        _jpeg = null;
        _label = "no image";
        PropertyChanged?.Invoke(this, new(nameof(Image)));
        PropertyChanged?.Invoke(this, new(nameof(Label)));
    }
}

/// <summary>Height of a 16:9 box for the given width.</summary>
public sealed class AspectHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double w && w > 0 && !double.IsInfinity(w) ? w * 9 / 16 : 0.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

internal sealed record GalleryEntry(double Duration, Thumbnail[] Images);

/// <summary>
/// Images kept in memory while viBro runs (never on disk), so F10 on a folder seen before is
/// immediate. Key: path, size, modification time and positions; a changed file gets new images.
/// Oldest entries are dropped beyond 64 MB of JPEG data.
/// </summary>
internal static class GalleryCache
{
    private const long MaxBytes = 64L * 1024 * 1024;
    private static readonly object Gate = new();
    private static readonly Dictionary<string, LinkedListNode<(string Key, GalleryEntry Entry, long Bytes)>> Map = new(StringComparer.OrdinalIgnoreCase);
    private static readonly LinkedList<(string Key, GalleryEntry Entry, long Bytes)> Order = new();
    private static long _bytes;

    public static string Key(VideoItem item, double[] percents) =>
        string.Join("|", item.FullPath, item.Size.ToString(CultureInfo.InvariantCulture),
            item.Modified.Ticks.ToString(CultureInfo.InvariantCulture),
            string.Join(",", percents.Select(p => p.ToString("0.#", CultureInfo.InvariantCulture))));

    public static bool TryGet(string key, out GalleryEntry entry)
    {
        lock (Gate)
        {
            if (Map.TryGetValue(key, out var node))
            {
                Order.Remove(node);
                Order.AddLast(node); // most recently used last
                entry = node.Value.Entry;
                return true;
            }
        }
        entry = null!;
        return false;
    }

    public static void Put(string key, GalleryEntry entry)
    {
        long bytes = entry.Images.Sum(i => (long)i.Jpeg.Length);
        lock (Gate)
        {
            if (Map.TryGetValue(key, out var old))
            {
                Order.Remove(old);
                _bytes -= old.Value.Bytes;
            }
            Map[key] = Order.AddLast((key, entry, bytes));
            _bytes += bytes;
            while (_bytes > MaxBytes && Order.First is { } first)
            {
                Order.RemoveFirst();
                Map.Remove(first.Value.Key);
                _bytes -= first.Value.Bytes;
            }
        }
    }
}
