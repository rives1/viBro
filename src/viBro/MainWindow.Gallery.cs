using System;
using System.IO;
using System.Linq;
using System.Windows;
using ViBro.Settings;
using System.Windows.Interop;
using System.Windows.Input;
using ViBro.Util;

namespace ViBro;

// Gallery (F10, 1.6.0): a separate window with one row of images per video of the current folder,
// made with the ffmpeg.exe of the extract. A click on a row selects and loads that video here; a
// click on an image loads it at the image's time.
public partial class MainWindow
{
    private GalleryWindow? _gallery;          // F10: the folder
    private GalleryWindow? _selectionGallery; // F12 (1.11.0): the selected videos

    /// <summary>Both galleries, for updates (rows of deleted or moved files) and for closing.</summary>
    private System.Collections.Generic.IEnumerable<GalleryWindow> OpenGalleries
        => new[] { _gallery, _selectionGallery }.OfType<GalleryWindow>();

    /// <summary>
    /// F12 (1.11.0): a gallery of the selected videos only, in list order, with the images of
    /// [SelectionGallery] PositionsPercent (10 % … 90 % and 99 % by default) in compact strips. Same
    /// clicks and right-click menu as the folder gallery; F12 again makes a new one.
    /// </summary>
    private void OpenSelectionGallery()
    {
        if (!FfmpegExtractor.IsAvailable)
        {
            SetStatus($"Gallery not available: put {FfmpegExtractor.ExeName} in the viBro folder ({AppInfo.ExeDirectory}).");
            return;
        }
        var videos = SelectedVideos;
        if (videos.Count == 0)
        {
            SetStatus("Select one or more videos in the list, then open the selection gallery.");
            return;
        }
        _selectionGallery?.Close();
        string title = $"Selection gallery - {(videos.Count == 1 ? "1 video" : videos.Count + " videos")} - {_currentFolder}";
        var g = new GalleryWindow(this, title, videos, _settings.SelectionGalleryPercents, _settings.GalleryJobs,
            _settings.GalleryCombined, compact: true);
        g.Closed += (_, _) =>
        {
            if (ReferenceEquals(_selectionGallery, g)) _selectionGallery = null;
        };
        _selectionGallery = g;
        g.Show();
    }

    private void OpenGallery()
    {
        if (!FfmpegExtractor.IsAvailable)
        {
            SetStatus($"Gallery not available: put {FfmpegExtractor.ExeName} in the viBro folder ({AppInfo.ExeDirectory}).");
            return;
        }
        if (_currentFolder == null)
        {
            SetStatus("No folder open: nothing to show in the gallery.");
            return;
        }
        var videos = _items.Where(i => i.IsVideo).ToList(); // in the list's order
        if (videos.Count == 0)
        {
            SetStatus("No videos in this folder: nothing to show in the gallery.");
            return;
        }

        _gallery?.Close(); // F10 again: a new gallery of the current folder
        var g = new GalleryWindow(this, "Gallery - " + _currentFolder, videos, _settings.GalleryPercents, _settings.GalleryJobs,
            _settings.GalleryCombined);
        g.Closed += (_, _) =>
        {
            if (ReferenceEquals(_gallery, g)) _gallery = null;
        };
        _gallery = g;
        g.Show();
    }

    /// <summary>
    /// From the gallery: selects the video in the list and loads it (play/pause kept); with a time,
    /// also moves to it (exactly). If another folder is open now, the gallery's folder is opened first.
    /// </summary>
    internal void ShowFromGallery(string path, double? seconds)
    {
        string? folder = Path.GetDirectoryName(path);
        if (folder == null) return;
        if (_currentFolder != null && string.Equals(Path.TrimEndingDirectorySeparator(folder),
                Path.TrimEndingDirectorySeparator(_currentFolder), StringComparison.OrdinalIgnoreCase))
        {
            SelectFromGallery(path, seconds);
            return;
        }
        OpenFolder(folder, revealInTree: true, onOpened: () => SelectFromGallery(path, seconds));
    }

    /// <summary>
    /// Right click in the gallery (1.8.0): the same menu as on the video, for that video (already
    /// selected and loaded by the gallery). Each action first makes sure that video, and only it, is
    /// selected here (the main window may show another folder or selection by then).
    /// </summary>
    internal void ShowGalleryFileMenu(string path, UIElement target)
    {
        var menu = BuildFileMenu(1, action => RunOnGalleryFile(path, action));
        menu.PlacementTarget = target;
        menu.IsOpen = true;
    }

    /// <summary>
    /// Keys pressed in a gallery (1.12.0): the commands of [Keys] that make sense for one video act on
    /// the gallery's selected row, with the same rules as in the main window (Delete asks for
    /// confirmation, as Canc / R do). Other keys are left to the gallery (row navigation).
    /// </summary>
    internal bool HandleGalleryKey(Key key, ModifierKeys mods, string path)
    {
        if (ComponentDispatcher.IsThreadModal || _fileOpBusy) return false;
        if (!_settings.Keys.TryGet(key, mods, out Command command)) return false;
        bool repeat = _win32KeyRepeat; // one key press = at most one operation
        Action? action = command switch
        {
            Command.Delete => () => RunOnGalleryFile(path, () => DeleteSelected(fromVideoMenu: false)),
            Command.Rename => () => RunOnGalleryFile(path, RenameSelected),
            Command.Move => () => RunOnGalleryFile(path, () => MoveSelected(useLastDestination: false)),
            Command.MoveToLast => () => RunOnGalleryFile(path, () => MoveSelected(useLastDestination: true)),
            Command.Copy => () => RunOnGalleryFile(path, () => CopySelected(useLastDestination: false)),
            Command.CopyToLast => () => RunOnGalleryFile(path, () => CopySelected(useLastDestination: true)),
            Command.PlayPause => () => _player?.TogglePause(),
            Command.Mute => () => _player?.ToggleMute(),
            _ => null,
        };
        if (action == null) return false;
        if (!repeat) Dispatcher.BeginInvoke(action); // after this key message (dialogs open on a clean state)
        return true;
    }

    private void RunOnGalleryFile(string path, Action action)
    {
        string? folder = Path.GetDirectoryName(path);
        if (folder == null) return;
        void Run()
        {
            var item = _items.Find(i => i.IsVideo && string.Equals(i.FullPath, path, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                SetStatus(Path.GetFileName(path) + " is no longer in the folder.");
                return;
            }
            SelectOnly(item);
            action();
        }
        if (_currentFolder != null && string.Equals(Path.TrimEndingDirectorySeparator(folder),
                Path.TrimEndingDirectorySeparator(_currentFolder), StringComparison.OrdinalIgnoreCase))
            Run();
        else
            OpenFolder(folder, revealInTree: true, onOpened: Run);
    }

    private void SelectFromGallery(string path, double? seconds)
    {
        var item = _items.Find(i => i.IsVideo && string.Equals(i.FullPath, path, StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            SetStatus(Path.GetFileName(path) + " is no longer in the folder.");
            return;
        }
        FileList.ScrollIntoView(item);

        bool loadedOrLoading = ReferenceEquals(item, _currentItem) || ReferenceEquals(item, _loadingItem);
        if (loadedOrLoading)
        {
            SelectOnly(item); // already loaded: select only (one row, also after a multiple selection)
            if (seconds is double s)
            {
                if (ReferenceEquals(item, _currentItem)) _player?.SeekAbsolute(s, exact: true);
                else
                {
                    _pendingStartItem = item; // still loading: move there when it is ready
                    _pendingStartPosition = s;
                }
            }
            return;
        }

        if (seconds is double t)
        {
            _pendingStartItem = item;
            _pendingStartPosition = t;
        }
        SelectAndLoad(item); // one row selected, the video loaded (play/pause kept)
    }
}
