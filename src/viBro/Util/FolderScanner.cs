using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace ViBro.Util;

/// <summary>Kind of a row in the file list. Parent ("..") and folders are listed before the videos.</summary>
internal enum EntryKind { Parent = 0, Folder = 1, Video = 2 }

/// <summary>
/// A row of the file list: a video, a subfolder or the parent folder (".."). Only videos are loaded,
/// counted and touched by file operations; folders open with a double click or Enter.
/// </summary>
internal sealed class VideoItem : System.ComponentModel.INotifyPropertyChanged
{
    // Segoe Fluent Icons: Play and Pause glyphs shown in front of the loaded file.
    private const string PlayGlyph = "\uE768", PauseGlyph = "\uE769";
    private bool _isCurrent, _isPaused = true;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The file loaded in the player (independent of the list selection).</summary>
    public bool IsCurrent => _isCurrent;

    /// <summary>Play glyph while playing, pause glyph while paused, empty when not loaded.</summary>
    public string StateGlyph => !_isCurrent ? "" : _isPaused ? PauseGlyph : PlayGlyph;

    public void SetCurrent(bool current, bool paused)
    {
        if (_isCurrent == current && _isPaused == paused) return;
        bool currentChanged = _isCurrent != current;
        _isCurrent = current;
        _isPaused = paused;
        if (currentChanged) PropertyChanged?.Invoke(this, new(nameof(IsCurrent)));
        PropertyChanged?.Invoke(this, new(nameof(StateGlyph)));
    }

    public VideoItem(string name, string fullPath, long size, DateTime modified, string storage, EntryKind kind = EntryKind.Video)
    {
        Kind = kind;
        Name = name;
        FullPath = fullPath;
        Size = size;
        Modified = modified;
        Storage = storage;
    }

    public string Name { get; }
    public string FullPath { get; }
    public long Size { get; }
    public DateTime Modified { get; }
    public string Storage { get; }
    public EntryKind Kind { get; }
    public bool IsVideo => Kind == EntryKind.Video;

    /// <summary>Folder icon for folders and "..", nothing for videos.</summary>
    public System.Windows.Media.ImageSource? Icon => IsVideo ? null : TreeIcons.Folder;

    public string SizeText => IsVideo ? FormatSize(Size) : "";
    public string ModifiedText => Kind == EntryKind.Parent ? "" : Modified.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);

    public static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double v = bytes;
        int u = 0;
        while (v >= 1024 && u < units.Length - 1)
        {
            v /= 1024;
            u++;
        }
        return u == 0 ? $"{bytes} B" : v.ToString(v >= 100 ? "0" : "0.0", CultureInfo.CurrentCulture) + " " + units[u];
    }
}

internal static class FolderScanner
{
    public static readonly HashSet<string> VideoExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mkv", ".mov", ".wmv" };

    /// <summary>
    /// Lists the video files and the subfolders of one folder (no recursion), plus ".." when the
    /// folder has a parent. Size and date come from the directory enumeration itself (one pass):
    /// no file is opened and no media metadata is read. Runs on a worker thread; on an unreachable
    /// share it may block for a long time, so callers must not wait for it on the UI thread.
    /// </summary>
    public static List<VideoItem> Scan(string folder, CancellationToken ct)
    {
        string storage = StorageClassifier.Classify(folder);
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
        };

        var list = new List<VideoItem>();
        string? parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(folder)); // string only
        if (!string.IsNullOrEmpty(parent))
            list.Add(new VideoItem("..", parent, 0, default, storage, EntryKind.Parent));
        foreach (var entry in new DirectoryInfo(folder).EnumerateFileSystemInfos("*", options))
        {
            ct.ThrowIfCancellationRequested();
            if (entry is DirectoryInfo di)
                list.Add(new VideoItem(di.Name, di.FullName, 0, di.LastWriteTime, storage, EntryKind.Folder));
            else if (entry is FileInfo fi && VideoExtensions.Contains(fi.Extension))
                list.Add(new VideoItem(fi.Name, fi.FullName, fi.Length, fi.LastWriteTime, storage));
        }
        list.Sort((a, b) => NaturalComparer.Instance.Compare(a.Name, b.Name));
        return list;
    }
}

internal static class StorageClassifier
{
    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>"unc", "mapped", "local", or another drive type; used only to label measurements.</summary>
    public static string Classify(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal)) return "unc";
        string? root;
        try { root = Path.GetPathRoot(path); }
        catch { return "unknown"; }
        if (string.IsNullOrEmpty(root)) return "unknown";

        return Cache.GetOrAdd(root, r =>
        {
            try
            {
                return new DriveInfo(r).DriveType switch
                {
                    DriveType.Network => "mapped",
                    DriveType.Fixed => "local",
                    var t => t.ToString().ToLowerInvariant(),
                };
            }
            catch
            {
                return "unknown";
            }
        });
    }
}
