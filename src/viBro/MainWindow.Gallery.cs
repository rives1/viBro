using System;
using System.IO;
using System.Linq;
using ViBro.Util;

namespace ViBro;

// Gallery (F10, 1.6.0): a separate window with one row of images per video of the current folder,
// made with the ffmpeg.exe of the extract. A click on a row selects and loads that video here; a
// click on an image loads it at the image's time.
public partial class MainWindow
{
    private GalleryWindow? _gallery;

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
        var g = new GalleryWindow(this, _currentFolder, videos, _settings.GalleryPercents, _settings.GalleryJobs);
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
