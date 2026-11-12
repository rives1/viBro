using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ViBro.Settings;
using ViBro.Util;

namespace ViBro;

// Filter of the file list (1.15.0): a box above the list, always visible. The list is filtered in
// memory (the items of the last scan, no new scan) through the list's collection view; the text is
// applied 200 ms after the last key. Words in AND, .ext, >N / <N (see Util/ListFilter). Only videos are
// filtered: folders and ".." stay. D / E, the wheel, the video loaded after delete / move, Ctrl+A and the
// F10 gallery work on the videos shown; selected rows that get hidden are deselected, so a file
// operation never touches a file that is not on screen. A new folder opens with an empty filter.
public partial class MainWindow
{
    private static readonly TimeSpan FilterDelay = TimeSpan.FromMilliseconds(200);

    private ListFilter _filter = ListFilter.None;
    private DispatcherTimer? _filterTimer;
    private bool _settingFilterText; // text changed by code: no delayed apply

    /// <summary>Name parts to highlight in the list (bound by the Name column).</summary>
    public static readonly DependencyProperty FilterWordsProperty = DependencyProperty.Register(
        nameof(FilterWords), typeof(IReadOnlyList<string>), typeof(MainWindow), new PropertyMetadata(Array.Empty<string>()));

    public IReadOnlyList<string> FilterWords
    {
        get => (IReadOnlyList<string>)GetValue(FilterWordsProperty);
        set => SetValue(FilterWordsProperty, value);
    }

    /// <summary>Folders and ".." always; videos when they pass the filter.</summary>
    private bool IsShown(VideoItem item) => !item.IsVideo || _filter.IsEmpty || _filter.Matches(item.Name, item.Size);

    /// <summary>Videos on screen, in list order.</summary>
    private IEnumerable<VideoItem> ShownVideos => _items.Where(i => i.IsVideo && IsShown(i));

    /// <summary>
    /// Gives the list its items (after any change of _items) with the current filter. Hidden rows leave
    /// the selection.
    /// </summary>
    private void BindItems()
    {
        FileList.ItemsSource = _items;
        ApplyViewFilter();
    }

    private void ApplyViewFilter()
    {
        bool old = _suppressSelectionLoad;
        _suppressSelectionLoad = true;
        try
        {
            FileList.Items.Filter = _filter.IsEmpty ? null : o => o is VideoItem v && IsShown(v);
            foreach (var hidden in FileList.SelectedItems.OfType<VideoItem>().Where(v => !IsShown(v)).ToList())
                FileList.SelectedItems.Remove(hidden);
        }
        finally
        {
            _suppressSelectionLoad = old;
        }
    }

    // ------------------------------------------------------------------ the box

    private void FilterBox_Loaded(object sender, RoutedEventArgs e)
        => FilterHint.Text = $"Filter  ·  name words   .mp4   >1G   <500M      ({_settings.Keys.Display(Command.Filter)})";

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterHint.Visibility = FilterBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        FilterClear.Visibility = FilterBox.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (_settingFilterText) return;
        _filterTimer ??= CreateFilterTimer();
        _filterTimer.Stop();
        _filterTimer.Start(); // debounce: applied 200 ms after the last change
    }

    private DispatcherTimer CreateFilterTimer()
    {
        var t = new DispatcherTimer(DispatcherPriority.Input) { Interval = FilterDelay };
        t.Tick += (_, _) =>
        {
            t.Stop();
            ApplyFilterText();
        };
        return t;
    }

    /// <summary>Enter / Down: to the list with the filter applied; Esc: clears it (or, empty, back to the list).</summary>
    private void FilterBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
            case Key.Down:
                ApplyFilterText();
                FocusListFromFilter();
                e.Handled = true;
                break;
            case Key.Escape:
                if (FilterBox.Text.Length == 0) FocusListFromFilter();
                else ClearFilter();
                e.Handled = true;
                break;
        }
    }

    private void FilterClear_Click(object sender, RoutedEventArgs e)
    {
        ClearFilter();
        FilterBox.Focus();
    }

    /// <summary>Ctrl+F: the cursor in the box, its text selected.</summary>
    private void FocusFilterBox()
    {
        FilterBox.Focus();
        FilterBox.SelectAll();
    }

    /// <summary>The list gets the keyboard: on the selected row, else on the loaded video, else on the first video shown.</summary>
    private void FocusListFromFilter()
    {
        var target = FileList.SelectedItem as VideoItem
                     ?? (_currentItem != null && IsShown(_currentItem) ? _currentItem : null)
                     ?? ShownVideos.FirstOrDefault();
        if (target != null && FileList.SelectedItems.Count == 0)
        {
            if (ReferenceEquals(target, _currentItem)) SelectOnly(target);
            else SelectAndLoad(target);
        }
        if (target != null && FileList.ItemContainerGenerator.ContainerFromItem(target) is ListViewItem row) row.Focus();
        else Keyboard.Focus(FileList);
    }

    // ------------------------------------------------------------------ applying

    private void ApplyFilterText()
    {
        _filterTimer?.Stop();
        var next = ListFilter.Parse(FilterBox.Text);
        if (next.Text == _filter.Text) return;
        SetFilter(next);
    }

    private void SetFilter(ListFilter filter)
    {
        _filter = filter;
        FilterWords = filter.Words.ToArray();
        ApplyViewFilter();
        var keep = FileList.SelectedItem ?? (_currentItem != null && IsShown(_currentItem) ? _currentItem : null);
        if (keep != null) FileList.ScrollIntoView(keep);
        UpdateFolderSummary();
    }

    /// <summary>Empties the box and shows every row (Esc, the ✕ button, a new folder, a gallery click on a hidden video).</summary>
    private void ClearFilter()
    {
        _filterTimer?.Stop();
        _settingFilterText = true;
        try
        {
            FilterBox.Text = "";
        }
        finally
        {
            _settingFilterText = false;
        }
        if (!_filter.IsEmpty) SetFilter(ListFilter.None);
    }

    /// <summary>
    /// From a gallery: the video must be on screen to be selected and acted on. If the filter hides it,
    /// the filter is cleared (with a message), never a hidden row selected.
    /// </summary>
    private void EnsureShown(VideoItem item)
    {
        if (IsShown(item)) return;
        ClearFilter();
        SetStatus($"Filter cleared: {item.Name} did not match it.");
    }

    /// <summary>Summary text for the filter, e.g. " · filter: 12 of 128".</summary>
    private string FilterSummary(int shownVideos, int allVideos)
        => _filter.IsEmpty ? "" : $" · filter: {shownVideos} of {allVideos}";
}

/// <summary>
/// The Name column's text with the parts matched by the filter highlighted (1.15.0). Set Text and
/// Words on a TextBlock; its inlines are rebuilt when either changes (also on recycled rows).
/// </summary>
public static class FilterHighlight
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text", typeof(string), typeof(FilterHighlight), new PropertyMetadata("", Changed));

    public static readonly DependencyProperty WordsProperty = DependencyProperty.RegisterAttached(
        "Words", typeof(IReadOnlyList<string>), typeof(FilterHighlight), new PropertyMetadata(null, Changed));

    public static string GetText(DependencyObject d) => (string)d.GetValue(TextProperty);
    public static void SetText(DependencyObject d, string value) => d.SetValue(TextProperty, value);
    public static IReadOnlyList<string>? GetWords(DependencyObject d) => (IReadOnlyList<string>?)d.GetValue(WordsProperty);
    public static void SetWords(DependencyObject d, IReadOnlyList<string>? value) => d.SetValue(WordsProperty, value);

    private static Brush? _mark;

    /// <summary>Windows' accent colour, see-through so the text stays readable in both themes.</summary>
    private static Brush Mark
    {
        get
        {
            if (_mark != null) return _mark;
            Color accent;
            try
            {
                accent = SystemColors.AccentColor;
            }
            catch (Exception)
            {
                accent = Color.FromRgb(0x00, 0x78, 0xD4);
            }
            var b = new SolidColorBrush(Color.FromArgb(0x66, accent.R, accent.G, accent.B));
            b.Freeze();
            return _mark = b;
        }
    }

    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb) return;
        string text = GetText(tb) ?? "";
        var words = GetWords(tb);
        var ranges = words == null || words.Count == 0 ? null : ListFilter.MatchRanges(text, words);
        if (ranges == null || ranges.Count == 0)
        {
            tb.Text = text; // the common case: one plain run
            return;
        }
        tb.Inlines.Clear();
        int at = 0;
        foreach (var (start, length) in ranges)
        {
            if (start > at) tb.Inlines.Add(new Run(text.Substring(at, start - at)));
            tb.Inlines.Add(new Run(text.Substring(start, length)) { Background = Mark });
            at = start + length;
        }
        if (at < text.Length) tb.Inlines.Add(new Run(text.Substring(at)));
    }
}
