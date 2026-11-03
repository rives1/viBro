using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace ViBro.Util;

internal sealed class VideoItem
{
    public VideoItem(string name, string fullPath, long size, DateTime modified, string storage)
    {
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

    public string SizeText => FormatSize(Size);
    public string ModifiedText => Modified.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);

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
    /// Lists the video files of one folder (no recursion). Size and date come from the
    /// directory enumeration itself: no file is opened and no media metadata is read.
    /// Runs on a worker thread; on an unreachable share it may block for a long time,
    /// so callers must not wait for it on the UI thread.
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
        foreach (var fi in new DirectoryInfo(folder).EnumerateFiles("*", options))
        {
            ct.ThrowIfCancellationRequested();
            if (VideoExtensions.Contains(fi.Extension))
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
