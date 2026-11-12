using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ViBro.Util;

namespace ViBro;

// F5 (1.8.0): refreshes the folder tree and the file list from the disk, keeping what the user
// is looking at: expanded folders, selection, loaded video, markers and scroll position.
public partial class MainWindow
{
    private static readonly TimeSpan TreeRefreshTimeout = TimeSpan.FromSeconds(10);

    private async void RefreshAll()
    {
        if (_fileOpBusy)
        {
            SetStatus("A file operation is running: press F5 when it has finished.", StatusKind.Error);
            return;
        }
        _ = _tree.RefreshAsync(TreeRefreshTimeout); // in the background; a slow share does not delay the list
        await RefreshListAsync();
    }

    /// <summary>
    /// Lists the current folder again. Rows of files still there are kept (same objects: selection,
    /// loaded video, markers and gallery stay valid) with their size and date updated; new files and
    /// folders are added, vanished ones removed. If the loaded video vanished, the player releases it.
    /// </summary>
    private async Task RefreshListAsync()
    {
        if (_currentFolder is not string path) return;
        int generation = ++_folderGeneration; // a folder opened meanwhile wins
        _folderCts?.Cancel();
        var cts = new CancellationTokenSource();
        _folderCts = cts;
        SetStatus("Refreshing " + path + " ...");

        List<VideoItem> scanned;
        try
        {
            scanned = await Task.Run(() => FolderScanner.Scan(path, cts.Token));
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            if (generation == _folderGeneration) SetStatus("Cannot refresh " + path + ": " + ex.Message, StatusKind.Error);
            return;
        }
        if (generation != _folderGeneration || _fileOpBusy) return; // superseded, or a file operation started

        var old = new Dictionary<string, VideoItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var i in _items) old.TryAdd(i.Kind + "|" + i.FullPath, i);
        var merged = new List<VideoItem>(scanned.Count);
        var fresh = new List<VideoItem>(); // 1.14.0: new rows, faded in
        int added = 0;
        foreach (var s in scanned)
        {
            if (old.Remove(s.Kind + "|" + s.FullPath, out var existing))
            {
                existing.UpdateInfo(s.Size, s.Modified);
                merged.Add(existing);
            }
            else
            {
                merged.Add(s);
                if (s.Kind != EntryKind.Parent) added++;
                fresh.Add(s);
            }
        }
        int removed = old.Values.Count(i => i.Kind != EntryKind.Parent);
        var gone = new HashSet<VideoItem>(old.Values);

        // 1.14.0: the vanished rows fade out first; meanwhile a folder change or a file operation wins.
        var faded = gone.Count > 0 ? await FadeOutRowsAsync(gone) : new List<UIElement>();
        if (generation != _folderGeneration || _fileOpBusy)
        {
            ClearFadedRows(faded);
            return;
        }

        var scroller = FindDescendant<ScrollViewer>(FileList);
        double offset = scroller?.VerticalOffset ?? 0;
        SetItems(merged, keepSelection: true); // sorted with the current column; the selection is kept
        ClearFadedRows(faded);
        if (scroller != null)
        {
            // Back to the same place now, so that the new rows that fade in are the ones on screen;
            // once more after the layout, as before 1.14.0.
            FileList.UpdateLayout();
            scroller.ScrollToVerticalOffset(offset);
            _ = Dispatcher.BeginInvoke(() => scroller.ScrollToVerticalOffset(offset), DispatcherPriority.Loaded);
        }
        FadeInRows(fresh);
        foreach (var g in OpenGalleries) g.RemoveVideos(gone.Where(i => i.IsVideo).Select(i => i.FullPath));
        if (_markItem != null && gone.Contains(_markItem))
        {
            _markItem = null;
            _markIn = _markOut = null;
            UpdateMarkerVisuals();
        }
        var loaded = _currentItem ?? _loadingItem;
        string extra = "";
        if (loaded != null && gone.Contains(loaded))
        {
            extra = $"  |  {loaded.Name} is no longer in the folder: unloaded";
            await ReleaseAsync(loaded);
            _playAfterRelease = null; // nothing is loaded next by F5
        }

        string changes = added == 0 && removed == 0
            ? "no changes"
            : $"{added} new, {removed} removed";
        SetStatus($"Refreshed {path}: {changes}{extra}", extra.Length > 0 ? StatusKind.Error : StatusKind.Normal);
    }

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            if (FindDescendant<T>(child) is T deeper) return deeper;
        }
        return null;
    }
}
