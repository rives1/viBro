using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using ViBro.Util;

namespace ViBro;

// Delete (Canc / R), rename (F2), move (W / Shift+W).
// Every operation acts on the selected file, releases it from the player first, updates the
// list only from what really happened on disk, and ignores key auto-repeat and re-entry.
public partial class MainWindow
{
    private bool _fileOpBusy;
    private VideoItem? _pendingStartItem;
    private double _pendingStartPosition;
    private string? _lastOpMessage;

    private VideoItem? SelectedVideo => FileList.SelectedItem as VideoItem;

    // ================================================================ delete

    private async void DeleteSelected()
    {
        if (_fileOpBusy || SelectedVideo is not VideoItem item) return;

        bool network = FileOps.IsNetworkPath(item.FullPath);
        string text = network
            ? $"Permanently delete this file?\n\n{item.Name}\n\nThe file is on a network location: it will be deleted " +
              "permanently, without the Recycle Bin, and cannot be recovered."
            : $"Move this file to the Recycle Bin?\n\n{item.Name}";
        var answer = MessageBox.Show(this, text, network ? "Delete permanently" : "Delete file",
            MessageBoxButton.YesNo, network ? MessageBoxImage.Warning : MessageBoxImage.Question, MessageBoxResult.Yes);
        if (answer != MessageBoxResult.Yes) return;

        _fileOpBusy = true;
        double position = _player?.TimePos ?? 0;
        try
        {
            await ReleaseAsync(item);

            DeleteOutcome outcome;
            string? error = null;
            try
            {
                if (network)
                {
                    outcome = await Task.Run(() => FileOps.DeletePermanently(item.FullPath));
                }
                else
                {
                    // Recycle Bin. If Windows cannot recycle the file it asks before deleting it permanently.
                    outcome = FileOps.Recycle(item.FullPath, new WindowInteropHelper(this).Handle, out error);
                }
            }
            catch (Exception ex)
            {
                outcome = await Task.Run(() => File.Exists(item.FullPath)) ? DeleteOutcome.NotDeleted : DeleteOutcome.Deleted;
                error = ex.Message;
            }

            if (outcome == DeleteOutcome.Deleted)
            {
                _lastOpMessage = $"Deleted {item.Name}" + (network ? " (permanently)" : " (Recycle Bin)");
                RemoveItemAndSelectNext(item);
            }
            else
            {
                SetStatus($"{item.Name} was not deleted" + (error != null ? ": " + error : "."));
                ReloadItem(item, position);
            }
        }
        finally
        {
            _fileOpBusy = false;
        }
    }

    // ================================================================ rename

    private async void RenameSelected()
    {
        if (_fileOpBusy || SelectedVideo is not VideoItem item) return;

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
                _lastOpMessage = $"Renamed to {renamed.Name}";
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

    // ================================================================ move

    private async void MoveSelected(bool useLastDestination)
    {
        if (_fileOpBusy || SelectedVideo is not VideoItem item) return;
        _fileOpBusy = true;
        try
        {
            await MoveCoreAsync(item, useLastDestination);
        }
        finally
        {
            _fileOpBusy = false;
        }
    }

    private async Task MoveCoreAsync(VideoItem item, bool useLastDestination)
    {
        // 1. Destination: Shift+W reuses the last one; W (or no stored destination) asks.
        string? dest = useLastDestination ? _settings.LastMoveDestination : null;
        while (true)
        {
            dest ??= ChooseDestination();
            if (dest == null) return;

            SetStatus("Checking destination " + dest + " ...");
            if (await IsFolderReachableAsync(dest, TimeSpan.FromSeconds(10))) break;

            var retry = MessageBox.Show(this, $"The destination folder is not reachable:\n\n{dest}\n\nChoose another folder?",
                "Move file", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);
            if (retry != MessageBoxResult.Yes)
            {
                SetStatus("Move cancelled.");
                return;
            }
            dest = null;
        }

        string sourceFolder = Path.GetDirectoryName(item.FullPath) ?? "";
        if (FileOps.IsSameFolder(dest, sourceFolder))
        {
            SetStatus("The file is already in that folder: nothing was moved.");
            return;
        }

        _settings.LastMoveDestination = dest;
        SaveSettingsNow();

        // 2. Name conflict: never overwrite silently.
        string name = item.Name;
        bool overwrite = false;
        if (await Task.Run(() => File.Exists(Path.Combine(dest, name))))
        {
            string keepBoth = await Task.Run(() => FileOps.UniqueName(dest, name));
            switch (ConflictDialog.Ask(this, name, dest, keepBoth))
            {
                case ConflictChoice.KeepBoth: name = keepBoth; break;
                case ConflictChoice.Overwrite: overwrite = true; break;
                default:
                    SetStatus("Move cancelled.");
                    return;
            }
        }

        // 3. Move. A progress window appears only if the transfer is not immediate.
        double position = _player?.TimePos ?? 0;
        using var cts = new CancellationTokenSource();
        ProgressDialog? progressDialog = null;
        var progress = new Progress<double>(p => progressDialog?.Report(p));
        try
        {
            await ReleaseAsync(item);
            SetStatus("Moving " + item.Name + " ...");
            var move = FileOps.MoveAsync(item.FullPath, dest, name, overwrite, progress, cts.Token);
            if (await Task.WhenAny(move, Task.Delay(400)) != move)
            {
                progressDialog = new ProgressDialog(this, item.Name, dest, cts);
                IsEnabled = false;
                progressDialog.Show();
            }
            string finalPath = await move;
            _lastOpMessage = $"Moved {item.Name} to {Path.GetDirectoryName(finalPath)}" +
                             (name != item.Name ? $" as {name}" : "");
            RemoveItemAndSelectNext(item);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Move cancelled: the file was not moved.");
            ReloadItem(item, position);
        }
        catch (Exception ex)
        {
            // Reflect what is really on disk: the source may or may not still exist.
            bool sourceExists = await Task.Run(() => File.Exists(item.FullPath));
            string message = ex is SourceNotDeletedException
                ? ex.Message
                : "The file was not moved.\n\n" + ex.Message;
            progressDialog = CloseProgress(progressDialog);
            MessageBox.Show(this, message, "Move file", MessageBoxButton.OK, MessageBoxImage.Warning);
            if (sourceExists) ReloadItem(item, position);
            else RemoveItemAndSelectNext(item);
        }
        finally
        {
            progressDialog = CloseProgress(progressDialog);
        }
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

    private string? ChooseDestination()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Move to folder" };
        string? initial = _settings.LastMoveDestination ?? _currentFolder;
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
            SetStatus("Settings not saved: " + ex.Message);
        }
    }

    // ================================================================ helpers

    /// <summary>Unloads the file from the player and waits until Windows has closed its handle.</summary>
    private async Task ReleaseAsync(VideoItem item)
    {
        _loadingItem = null;
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

        _suppressSelectionLoad = true;
        try
        {
            FileList.SelectedItem = newItem;
            FileList.ScrollIntoView(newItem);
        }
        finally
        {
            _suppressSelectionLoad = false;
        }
    }

    /// <summary>After delete/move: select and load (paused) the next file, or the previous if it was the last.</summary>
    private void RemoveItemAndSelectNext(VideoItem item)
    {
        var list = new List<VideoItem>(_items);
        int index = list.IndexOf(item);
        if (index < 0) return;
        list.RemoveAt(index);

        _suppressSelectionLoad = true;
        try
        {
            _items = list;
            FileList.ItemsSource = _items;
        }
        finally
        {
            _suppressSelectionLoad = false;
        }

        if (_items.Count == 0)
        {
            // The player has already released the file: the video area stays empty.
            SetStatus((_lastOpMessage ?? "") + "  |  No more videos in this folder.");
            _lastOpMessage = null;
            return;
        }

        int next = Math.Min(index, _items.Count - 1);
        FileList.SelectedIndex = next; // loads it, paused
        FileList.ScrollIntoView(_items[next]);
    }

    /// <summary>Called when a load completes: restores the position after a reload, shows the last operation.</summary>
    private string ApplyPostLoadActions(VideoItem item, string status)
    {
        if (ReferenceEquals(item, _pendingStartItem))
        {
            if (_pendingStartPosition > 0.5) _player?.SeekAbsolute(_pendingStartPosition, exact: true);
            _pendingStartItem = null;
        }
        if (_lastOpMessage != null)
        {
            status = _lastOpMessage + "  |  " + status;
            _lastOpMessage = null;
        }
        return status;
    }
}
