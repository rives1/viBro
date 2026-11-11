using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using ViBro.Util;

namespace ViBro;

// Delete (Canc / R), rename (F2), move (W / Shift+W), copy (C / Shift+C).
// Since 1.7.0 delete, move and copy act on every selected video (Ctrl / Shift + click, Ctrl+A);
// rename and the other single-file commands need exactly one selected video.
// Every operation releases the loaded file from the player first when it is involved, updates the
// list only from what really happened on disk, and ignores key auto-repeat and re-entry.
public partial class MainWindow
{
    private const int MaxNamesShown = 8;

    private bool _fileOpBusy;
    // 1.12.0: files being deleted or moved; they are not loaded until the operation ends (a file opened
    // by the player meanwhile would make its own deletion, or the removal of a moved source, fail).
    private readonly HashSet<string> _busyPaths = new(StringComparer.OrdinalIgnoreCase);
    private VideoItem? _pendingStartItem;
    private double _pendingStartPosition;
    private string? _lastOpMessage;
    private StatusKind _lastOpKind = StatusKind.Success; // colour of _lastOpMessage (1.13.0)
    private bool? _playAfterRelease; // playback state before a file operation released the player

    /// <summary>Selected videos in list order (folders and ".." are never touched by file operations).</summary>
    private List<VideoItem> SelectedVideos
    {
        get
        {
            var set = new HashSet<VideoItem>(FileList.SelectedItems.OfType<VideoItem>().Where(v => v.IsVideo));
            return set.Count == 0 ? new List<VideoItem>() : _items.Where(set.Contains).ToList();
        }
    }

    /// <summary>The selected video when exactly one video is selected.</summary>
    private VideoItem? SelectedVideo
    {
        get
        {
            var v = SelectedVideos;
            return v.Count == 1 ? v[0] : null;
        }
    }

    /// <summary>Single-file commands (F2, Shift+Enter, I / O / F9) are disabled with several videos selected.</summary>
    private bool RejectMultipleSelection(string command)
    {
        int n = SelectedVideos.Count;
        if (n <= 1) return false;
        SetStatus($"{command} works on one file: {n} videos are selected, select only one.", StatusKind.Error);
        return true;
    }

    private static string FilesText(int n) => n == 1 ? "1 file" : $"{n} files";

    /// <summary>The first names, one per line, then "... and N more".</summary>
    private static string NameList(IReadOnlyList<VideoItem> items)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < items.Count && i < MaxNamesShown; i++) sb.AppendLine(items[i].Name);
        if (items.Count > MaxNamesShown) sb.AppendLine($"... and {items.Count - MaxNamesShown} more");
        return sb.ToString().TrimEnd();
    }

    private void ShowErrors(string title, string intro, List<(string Name, string Error)> errors)
    {
        if (errors.Count == 0) return;
        var shown = errors.Take(MaxNamesShown).Select(e => e.Name + ": " + e.Error).ToList();
        if (errors.Count > MaxNamesShown) shown.Add($"... and {errors.Count - MaxNamesShown} more (see viBro-errors.log)");
        foreach (var e in errors) ErrorLog.Write("fileops", $"{title}: {e.Name}: {e.Error}");
        MessageBox.Show(this, intro + "\n\n" + string.Join("\n", shown), title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // ================================================================ delete

    /// <summary>
    /// Canc / R and the video menu. From the video menu (1.8.0) a single file is deleted without the
    /// confirmation, local or on the network, unless [Delete] ConfirmFromVideoMenu=true; several files
    /// are always confirmed.
    /// </summary>
    private async void DeleteSelected(bool fromVideoMenu)
    {
        var items = SelectedVideos;
        if (_fileOpBusy || items.Count == 0) return;
        int n = items.Count;
        bool confirm = !(fromVideoMenu && n == 1 && !_settings.ConfirmDeleteFromVideoMenu);

        bool network = FileOps.IsNetworkPath(items[0].FullPath); // all in the current folder
        string text = n == 1
            ? network
                ? $"Permanently delete this file?\n\n{items[0].Name}\n\nThe file is on a network location: it will be deleted " +
                  "permanently, without the Recycle Bin, and cannot be recovered."
                : $"Move this file to the Recycle Bin?\n\n{items[0].Name}"
            : network
                ? $"Permanently delete these {n} files?\n\n{NameList(items)}\n\nThe files are on a network location: they will be " +
                  "deleted permanently, without the Recycle Bin, and cannot be recovered."
                : $"Move these {n} files to the Recycle Bin?\n\n{NameList(items)}";
        string title = n == 1
            ? (network ? "Delete permanently" : "Delete file")
            : (network ? $"Delete {n} files permanently" : $"Delete {n} files");
        // viBro's own dialog, opened next to the mouse pointer (a MessageBox cannot be positioned).
        if (confirm && !DeleteConfirmDialog.Ask(this, title, text, warning: network)) return;

        _fileOpBusy = true;
        _busyPaths.UnionWith(items.Select(i => i.FullPath));
        try
        {
            var (released, position) = await ReleaseIfLoadedAsync(items);
            var deleted = new List<VideoItem>();
            var errors = new List<(string Name, string Error)>();
            for (int i = 0; i < n; i++)
            {
                var item = items[i];
                if (n > 1) SetStatus($"Deleting {i + 1} / {n}: {item.Name} ...");
                await System.Windows.Threading.Dispatcher.Yield(); // keep the window responsive between files
                DeleteOutcome outcome;
                string? error = null;
                try
                {
                    if (network)
                        outcome = await Task.Run(() => FileOps.DeletePermanently(item.FullPath));
                    else
                        // Recycle Bin. If Windows cannot recycle the file it asks before deleting it permanently.
                        outcome = FileOps.Recycle(item.FullPath, new WindowInteropHelper(this).Handle, out error);
                }
                catch (Exception ex)
                {
                    outcome = await Task.Run(() => File.Exists(item.FullPath)) ? DeleteOutcome.NotDeleted : DeleteOutcome.Deleted;
                    error = ex.Message;
                }
                if (outcome == DeleteOutcome.Deleted) deleted.Add(item);
                else errors.Add((item.Name, error ?? "not deleted"));
            }

            string how = network ? " (permanently)" : " (Recycle Bin)";
            if (n == 1)
            {
                if (deleted.Count == 1) (_lastOpMessage, _lastOpKind) = ($"Deleted {items[0].Name}{how}", StatusKind.Success);
                else (_lastOpMessage, _lastOpKind) = ($"{items[0].Name} was not deleted: {errors[0].Error}", StatusKind.Error);
            }
            else
            {
                _lastOpMessage = $"Deleted {deleted.Count} of {n} files{how}";
                _lastOpKind = errors.Count == 0 ? StatusKind.Success : StatusKind.Error;
                ShowErrors("Delete files", $"{FilesText(errors.Count)} not deleted:", errors);
            }
            _busyPaths.Clear(); // the files not deleted can be loaded again
            AfterRemoval(deleted, released, position);
        }
        finally
        {
            _busyPaths.Clear();
            _fileOpBusy = false;
        }
    }

    // ================================================================ rename

    private async void RenameSelected()
    {
        if (_fileOpBusy || RejectMultipleSelection("Rename") || SelectedVideo is not VideoItem item) return;

        string? proposedStem = null;
        string? error = null;
        while (true)
        {
            var dialog = new RenameDialog(this, item.Name, proposedStem, error);
            if (dialog.ShowDialog() != true || dialog.ResultName == null) return;
            string newName = dialog.ResultName;
            if (string.Equals(newName, item.Name, StringComparison.Ordinal)) return; // unchanged

            _fileOpBusy = true;
            double position = _player?.TimePos ?? 0;
            try
            {
                await ReleaseAsync(item);
                string newPath = await Task.Run(() => FileOps.Rename(item.FullPath, newName));
                var renamed = new VideoItem(Path.GetFileName(newPath), newPath, item.Size, item.Modified, item.Storage);
                ReplaceItem(item, renamed);
                (_lastOpMessage, _lastOpKind) = ($"Renamed to {renamed.Name}", StatusKind.Success);
                ReloadItem(renamed, position);
                return;
            }
            catch (Exception ex)
            {
                // Nothing changed on disk: reload the original and let the user correct the name.
                ReloadItem(item, position);
                proposedStem = Path.GetFileNameWithoutExtension(newName);
                error = ex.Message;
            }
            finally
            {
                _fileOpBusy = false;
            }
        }
    }

    // ================================================================ move and copy

    private async void MoveSelected(bool useLastDestination) => await TransferSelectedAsync(useLastDestination, copy: false);

    /// <summary>C / Shift+C and the video menu: copy with the same rules as the move.</summary>
    private async void CopySelected(bool useLastDestination) => await TransferSelectedAsync(useLastDestination, copy: true);

    private async Task TransferSelectedAsync(bool useLastDestination, bool copy)
    {
        var items = SelectedVideos;
        if (_fileOpBusy || items.Count == 0) return;
        _fileOpBusy = true;
        try
        {
            await TransferAsync(items, useLastDestination, copy);
        }
        finally
        {
            _fileOpBusy = false;
        }
    }

    /// <summary>Destination (last one or chosen), reachability and same folder. Null when cancelled.</summary>
    private async Task<string?> PrepareDestinationAsync(bool useLastDestination, bool copy, string sourceFolder, int count)
    {
        string op = copy ? "Copy" : "Move";
        string? dest = useLastDestination ? (copy ? _settings.LastCopyDestination : _settings.LastMoveDestination) : null;
        while (true)
        {
            dest ??= ChooseDestination(copy);
            if (dest == null) return null;

            SetStatus("Checking destination " + dest + " ...");
            if (await IsFolderReachableAsync(dest, TimeSpan.FromSeconds(10))) break;

            var retry = MessageBox.Show(this, $"The destination folder is not reachable:\n\n{dest}\n\nChoose another folder?",
                op + (count == 1 ? " file" : " files"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);
            if (retry != MessageBoxResult.Yes)
            {
                SetStatus(op + " cancelled.");
                return null;
            }
            dest = null;
        }

        if (FileOps.IsSameFolder(dest, sourceFolder))
        {
            SetStatus(count == 1
                ? $"The file is already in that folder: nothing was {(copy ? "copied" : "moved")}."
                : $"The files are already in that folder: nothing was {(copy ? "copied" : "moved")}.");
            return null;
        }

        if (copy) _settings.LastCopyDestination = dest;
        else _settings.LastMoveDestination = dest;
        SaveSettingsNow();
        return dest;
    }

    /// <summary>
    /// Name conflicts, asked before anything is transferred: one question per conflicting file, with
    /// "Apply to all" for the next ones (several files). Never overwrites silently. Null = cancelled.
    /// </summary>
    private async Task<List<(VideoItem Item, string Name, bool Overwrite)>?> PlanTransferAsync(
        List<VideoItem> items, string dest, bool copy)
    {
        string op = copy ? "Copy" : "Move";
        bool[] exists = await Task.Run(() => items.Select(i => File.Exists(Path.Combine(dest, i.Name))).ToArray());
        int conflicts = exists.Count(e => e), seen = 0;
        // Names the transfer will create: a "keep both" name must not take the name of another file being transferred.
        var reserved = new HashSet<string>(items.Select(i => i.Name), StringComparer.OrdinalIgnoreCase);
        var plan = new List<(VideoItem, string, bool)>();
        ConflictChoice? forAll = null;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!exists[i])
            {
                plan.Add((item, item.Name, false));
                continue;
            }
            seen++;
            string keepBoth = await Task.Run(() => FileOps.UniqueName(dest, item.Name, reserved));
            bool applyToAll = false;
            ConflictChoice choice = forAll ?? ConflictDialog.Ask(this, item.Name, dest, keepBoth, copy,
                items.Count > 1, conflicts - seen, out applyToAll);
            if (forAll == null && applyToAll) forAll = choice;
            switch (choice)
            {
                case ConflictChoice.KeepBoth:
                    reserved.Add(keepBoth);
                    plan.Add((item, keepBoth, false));
                    break;
                case ConflictChoice.Overwrite:
                    plan.Add((item, item.Name, true));
                    break;
                case ConflictChoice.Skip:
                    break;
                default:
                    SetStatus(op + " cancelled: nothing was " + (copy ? "copied." : "moved."));
                    return null;
            }
        }
        return plan;
    }

    /// <summary>
    /// Move or copy of the selected videos, one after the other, with one progress window for all
    /// (by size) when it is not immediate. Cancelling stops at the file in progress: the files already
    /// done stay done, the one in progress is left as it was (a move keeps its source until the copy is
    /// complete). Errors on one file do not stop the others; they are listed at the end.
    /// A copy only reads the sources, so the player keeps the loaded file open and keeps playing.
    /// </summary>
    private async Task TransferAsync(List<VideoItem> items, bool useLastDestination, bool copy)
    {
        int n = items.Count;
        string op = copy ? "Copy" : "Move";
        string sourceFolder = Path.GetDirectoryName(items[0].FullPath) ?? "";
        string? dest = await PrepareDestinationAsync(useLastDestination, copy, sourceFolder, n);
        if (dest == null) return;
        var plan = await PlanTransferAsync(items, dest, copy);
        if (plan == null) return;
        if (plan.Count == 0)
        {
            SetStatus($"{op}: all files skipped, nothing was {(copy ? "copied" : "moved")}.");
            return;
        }

        if (!copy) _busyPaths.UnionWith(plan.Select(p => p.Item.FullPath)); // a copy only reads: its files stay loadable
        var (released, position) = copy ? ((VideoItem?)null, 0.0) : await ReleaseIfLoadedAsync(plan.Select(p => p.Item).ToList());
        var done = new List<(VideoItem Item, string FinalPath)>();
        var removed = new List<VideoItem>(); // moves whose source is gone (moved, or copied with the source deleted)
        var errors = new List<(string Name, string Error)>();
        bool cancelled = false;
        Exception? firstError = null;

        using var cts = new CancellationTokenSource();
        long totalBytes = Math.Max(1, plan.Sum(p => Math.Max(0, p.Item.Size)));
        long doneBytes = 0;
        double lastFraction = 0;
        ProgressDialog? progressDialog = null;
        string FileLine(int k) => $"file {k + 1} of {plan.Count}: \"{plan[k].Item.Name}\"";
        int current = 0;
        bool finished = false;

        // The progress window appears only if the whole transfer is not immediate.
        _ = Task.Delay(400).ContinueWith(_ =>
        {
            if (finished || progressDialog != null) return;
            progressDialog = new ProgressDialog(this, plan[current].Item.Name, dest, cts, copy, files: plan.Count);
            if (plan.Count > 1) progressDialog.SetFile(FileLine(current));
            progressDialog.Report(lastFraction);
            IsEnabled = false;
            progressDialog.Show();
        }, TaskScheduler.FromCurrentSynchronizationContext());

        try
        {
            for (current = 0; current < plan.Count; current++)
            {
                var (item, name, overwrite) = plan[current];
                long size = Math.Max(0, item.Size), before = doneBytes;
                if (plan.Count > 1) progressDialog?.SetFile(FileLine(current));
                var progress = new Progress<double>(p =>
                {
                    lastFraction = (before + p * size) / (double)totalBytes;
                    progressDialog?.Report(lastFraction);
                });
                SetStatus((copy ? "Copying " : "Moving ") + (plan.Count > 1 ? $"{current + 1} / {plan.Count}: " : "") + item.Name + " ...");
                try
                {
                    string finalPath = copy
                        ? await FileOps.CopyAsync(item.FullPath, dest, name, overwrite, progress, cts.Token)
                        : await FileOps.MoveAsync(item.FullPath, dest, name, overwrite, progress, cts.Token);
                    done.Add((item, finalPath));
                    if (!copy) removed.Add(item);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    break;
                }
                catch (Exception ex)
                {
                    errors.Add((item.Name, ex.Message));
                    firstError ??= ex;
                    // Reflect what is really on disk: after a failed move the source may be gone (copied, then deleted).
                    if (!copy && !await Task.Run(() => File.Exists(item.FullPath))) removed.Add(item);
                }
                doneBytes += size;
            }
        }
        finally
        {
            finished = true;
            _busyPaths.Clear();
            progressDialog = CloseProgress(progressDialog);
        }

        // Messages: for one file as before 1.7.0; for several, a count and the list of errors.
        string message;
        if (n == 1 && plan.Count == 1)
        {
            var (item, name, _) = plan[0];
            if (done.Count == 1)
                message = $"{(copy ? "Copied" : "Moved")} {item.Name} to {Path.GetDirectoryName(done[0].FinalPath)}" +
                          (name != item.Name ? $" as {name}" : "");
            else if (cancelled)
                message = copy ? "Copy cancelled: nothing was copied." : "Move cancelled: the file was not moved.";
            else
            {
                string detail = errors.Count > 0 ? errors[0].Error : "unknown error";
                message = $"{op} failed: {detail}";
                // Copied but the original could not be deleted: the message says so itself.
                string intro = firstError is SourceNotDeletedException ? "" : copy ? "The file was not copied.\n\n" : "The file was not moved.\n\n";
                MessageBox.Show(this, intro + detail,
                    op + " file", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        else
        {
            int skipped = n - plan.Count;
            message = $"{(copy ? "Copied" : "Moved")} {done.Count} of {n} files to {dest}" +
                      (skipped > 0 ? $" ({skipped} skipped)" : "") +
                      (cancelled ? " - cancelled, the remaining files were not " + (copy ? "copied" : "moved") : "");
            ShowErrors(op + " files", $"{FilesText(errors.Count)} not {(copy ? "copied" : "moved")}:", errors);
        }

        StatusKind kind = errors.Count > 0 ? StatusKind.Error : cancelled || done.Count == 0 ? StatusKind.Normal : StatusKind.Success;
        if (copy)
        {
            SetStatus(message, kind);
            return;
        }
        (_lastOpMessage, _lastOpKind) = (message, kind);
        AfterRemoval(removed, released, position);
    }

    private ProgressDialog? CloseProgress(ProgressDialog? dialog)
    {
        IsEnabled = true;
        if (dialog != null)
        {
            dialog.Finish();
            Activate();
        }
        return null;
    }

    private string? ChooseDestination(bool copy)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = copy ? "Copy to folder" : "Move to folder" };
        string? initial = (copy ? _settings.LastCopyDestination : _settings.LastMoveDestination) ?? _currentFolder;
        if (!string.IsNullOrEmpty(initial)) dialog.InitialDirectory = initial;
        return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
    }

    private static async Task<bool> IsFolderReachableAsync(string folder, TimeSpan timeout)
    {
        var check = Task.Run(() => Directory.Exists(folder));
        return await Task.WhenAny(check, Task.Delay(timeout)) == check && check.Result;
    }

    private void SaveSettingsNow()
    {
        if (!_iniWritable) return;
        try
        {
            _settings.Save(_iniPath);
        }
        catch (Exception ex)
        {
            SetStatus("Settings not saved: " + ex.Message, StatusKind.Error);
        }
    }

    // ================================================================ helpers

    /// <summary>
    /// Releases the player only if the loaded (or loading) file is one of <paramref name="items"/>;
    /// otherwise it keeps playing. Returns the released file and its position.
    /// </summary>
    private async Task<(VideoItem? Released, double Position)> ReleaseIfLoadedAsync(List<VideoItem> items)
    {
        var loaded = _currentItem ?? _loadingItem;
        if (loaded == null || !items.Contains(loaded)) return (null, 0);
        double position = _player?.TimePos ?? 0;
        await ReleaseAsync(loaded);
        return (loaded, position);
    }

    /// <summary>Unloads the file from the player and waits until Windows has closed its handle.</summary>
    private async Task ReleaseAsync(VideoItem item)
    {
        // The next file loaded after this operation keeps the current play/pause state.
        _playAfterRelease = _player != null && !_player.IsPaused;
        _loadingItem = null;
        SetCurrentItem(null); // nothing is loaded while the file operation runs
        _currentInfo = null;
        if (_player != null)
            await Task.WhenAny(_player.ReleaseFileAsync(), Task.Delay(3000));
        if (!await FileOps.WaitUntilReleasedAsync(item.FullPath, TimeSpan.FromSeconds(5)))
            SetStatus("The file is still in use by another program; trying anyway ...");
    }

    /// <summary>Reloads a file paused, then returns to the previous position.</summary>
    private void ReloadItem(VideoItem item, double position)
    {
        _pendingStartItem = item;
        _pendingStartPosition = position;
        LoadItem(item);
    }

    private void ReplaceItem(VideoItem oldItem, VideoItem newItem)
    {
        var list = new List<VideoItem>(_items);
        int i = list.IndexOf(oldItem);
        if (i >= 0) list[i] = newItem;
        else list.Add(newItem);
        SetItems(list, keepSelection: false);
        SelectOnly(newItem);
    }

    /// <summary>Selects one row only (others deselected), without loading it.</summary>
    private void SelectOnly(VideoItem item)
    {
        _suppressSelectionLoad = true;
        try
        {
            FileList.SelectedItems.Clear();
            FileList.SelectedItem = item;
            FileList.ScrollIntoView(item);
        }
        finally
        {
            _suppressSelectionLoad = false;
        }
        UpdateFolderSummary();
    }

    /// <summary>Selects one row only and loads it (play/pause kept), unless it is already loaded or loading.</summary>
    private void SelectAndLoad(VideoItem item)
    {
        SelectOnly(item);
        if (!ReferenceEquals(item, _currentItem) && !ReferenceEquals(item, _loadingItem)) LoadItem(item);
    }

    /// <summary>
    /// After delete/move. The list loses the files really removed; then:
    /// - the released (loaded) file is still there (not removed): it is selected and reloaded at its position;
    /// - the loaded file was not involved: it stays loaded and selected;
    /// - otherwise the video after the last removed one is selected and loaded (play/pause kept), or the
    ///   previous one if none follows; with no videos left the video area stays empty.
    /// </summary>
    private void AfterRemoval(List<VideoItem> removed, VideoItem? released, double position)
    {
        var old = _items;
        var gone = new HashSet<VideoItem>(removed);
        int last = -1;
        for (int i = 0; i < old.Count; i++) if (gone.Contains(old[i])) last = i;

        if (gone.Count > 0)
        {
            foreach (var g in OpenGalleries) g.RemoveVideos(removed.Select(i => i.FullPath)); // their rows leave the galleries too
            _suppressSelectionLoad = true;
            try
            {
                _items = old.Where(i => !gone.Contains(i)).ToList();
                FileList.ItemsSource = _items;
            }
            finally
            {
                _suppressSelectionLoad = false;
            }
            UpdateFolderSummary();
        }

        if (released != null && !gone.Contains(released))
        {
            SelectOnly(released);
            ReloadItem(released, position); // shows _lastOpMessage when loaded
            return;
        }
        if (released == null && _currentItem != null && _items.Contains(_currentItem))
        {
            SelectOnly(_currentItem); // the loaded file was not involved: it keeps playing
            if (_lastOpMessage != null) SetStatus(_lastOpMessage, _lastOpKind);
            _lastOpMessage = null;
            return;
        }

        // The video that followed the last removed one, otherwise the closest one before it (folders are skipped).
        VideoItem? next = null;
        if (last >= 0)
        {
            for (int i = last + 1; i < old.Count && next == null; i++) if (old[i].IsVideo && !gone.Contains(old[i])) next = old[i];
            for (int i = last - 1; i >= 0 && next == null; i--) if (old[i].IsVideo && !gone.Contains(old[i])) next = old[i];
        }
        if (next == null)
        {
            _playAfterRelease = null; // nothing is loaded next: do not carry the state further
            // The player has already released the file: the video area stays empty.
            SetStatus((_lastOpMessage ?? "") + (_items.Any(i => i.IsVideo) ? "" : "  |  No more videos in this folder."),
                _lastOpMessage != null ? _lastOpKind : StatusKind.Normal);
            _lastOpMessage = null;
            return;
        }
        SelectAndLoad(next);
        if (ReferenceEquals(next, _currentItem) && _lastOpMessage != null)
        {
            SetStatus(_lastOpMessage, _lastOpKind); // already loaded: no load will show the message
            _lastOpMessage = null;
        }
    }

    /// <summary>
    /// Called when a load completes: restores the position after a reload and shows the result of the
    /// last file operation (in its colour), or clears "Loading ..." (1.13.0: the file name and format
    /// are no longer repeated in the message area).
    /// </summary>
    private void ApplyPostLoadActions(VideoItem item)
    {
        if (ReferenceEquals(item, _pendingStartItem))
        {
            if (_pendingStartPosition > 0.5) _player?.SeekAbsolute(_pendingStartPosition, exact: true);
            _pendingStartItem = null;
        }
        if (_lastOpMessage != null)
        {
            SetStatus(_lastOpMessage, _lastOpKind);
            _lastOpMessage = null;
        }
        else SetStatus("");
    }
}
