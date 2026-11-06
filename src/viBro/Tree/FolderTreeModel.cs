using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ViBro.Tree;

/// <summary>
/// Roots of the folder tree: user folders (Desktop, Documents, Videos, Downloads), all drives
/// (including mapped network drives) and, for the session, any UNC share opened from the path bar.
/// Building the roots does no I/O on the calling thread; drive labels arrive later.
/// </summary>
internal sealed class FolderTreeModel
{
    public ObservableCollection<FolderNode> Roots { get; } = new();

    public static FolderTreeModel CreateDefault()
    {
        var m = new FolderTreeModel();
        m.AddUserFolder("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        m.AddUserFolder("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        m.AddUserFolder("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        m.AddUserFolder("Downloads", GetDownloadsFolder());
        m.AddDrives();
        return m;
    }

    public void AddUserFolder(string name, string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        Roots.Add(new FolderNode(name, PathUtil.NormalizeFolder(path), FolderNodeKind.UserFolder));
    }

    private void AddDrives()
    {
        DriveInfo[] drives;
        try { drives = DriveInfo.GetDrives(); } // GetLogicalDrives: no access to the drives themselves
        catch { return; }

        foreach (var d in drives) AddDrive(d);
    }

    private void AddDrive(DriveInfo d)
    {
        DriveType type;
        try { type = d.DriveType; } catch { type = DriveType.Unknown; }
        var kind = type switch
        {
            DriveType.Network => FolderNodeKind.NetworkDrive,
            DriveType.Removable => FolderNodeKind.RemovableDrive,
            DriveType.CDRom => FolderNodeKind.OpticalDrive,
            _ => FolderNodeKind.Drive,
        };
        string letter = d.Name.TrimEnd('\\'); // "C:"
        var node = new FolderNode(DefaultDriveLabel(kind, letter), d.Name, kind);
        Roots.Add(node);
        if (kind != FolderNodeKind.OpticalDrive)
            _ = LoadDriveLabelAsync(node, d, letter);
    }

    private static string DefaultDriveLabel(FolderNodeKind kind, string letter) => kind switch
    {
        FolderNodeKind.NetworkDrive => $"Network drive ({letter})",
        FolderNodeKind.RemovableDrive => $"Removable drive ({letter})",
        FolderNodeKind.OpticalDrive => $"Optical drive ({letter})",
        _ => $"Local disk ({letter})",
    };

    /// <summary>Volume label read in the background: on a disconnected share it can take long or fail.</summary>
    private static async Task LoadDriveLabelAsync(FolderNode node, DriveInfo drive, string letter)
    {
        try
        {
            string? label = await Task.Run(() => drive.IsReady ? drive.VolumeLabel : null);
            if (!string.IsNullOrWhiteSpace(label)) node.Name = $"{label} ({letter})";
        }
        catch
        {
            // keep the default label
        }
    }

    /// <summary>
    /// F5 (1.8.0): drives that appeared (e.g. a USB stick) are added after the others, drives that
    /// disappeared are removed; then every listed node is refreshed (FolderNode.RefreshAsync).
    /// </summary>
    public async Task RefreshAsync(TimeSpan timeout)
    {
        DriveInfo[] drives;
        try { drives = DriveInfo.GetDrives(); }
        catch { drives = Array.Empty<DriveInfo>(); }
        if (drives.Length > 0)
        {
            var present = new HashSet<string>(drives.Select(d => d.Name), StringComparer.OrdinalIgnoreCase);
            for (int i = Roots.Count - 1; i >= 0; i--)
            {
                var r = Roots[i];
                bool isDrive = r.Kind is FolderNodeKind.Drive or FolderNodeKind.NetworkDrive or FolderNodeKind.RemovableDrive or FolderNodeKind.OpticalDrive;
                if (isDrive && !present.Contains(r.Path)) Roots.RemoveAt(i);
            }
            var known = new HashSet<string>(Roots.Select(r => r.Path), StringComparer.OrdinalIgnoreCase);
            foreach (var d in drives)
                if (!known.Contains(d.Name)) AddDrive(d);
        }
        await Task.WhenAll(Roots.ToList().Select(r => r.RefreshAsync(timeout)));
    }

    /// <summary>Best root for a path: the deepest root that contains it. UNC shares get a root on demand.</summary>
    public FolderNode? FindOrAddRoot(string folder)
    {
        FolderNode? best = null;
        foreach (var r in Roots)
        {
            if (PathUtil.IsSameOrInside(folder, r.Path) && (best == null || r.Path.Length > best.Path.Length))
                best = r;
        }
        if (best != null) return best;

        string? share = PathUtil.GetUncShareRoot(folder);
        if (share == null) return null;
        var node = new FolderNode(share, share, FolderNodeKind.NetworkShare);
        Roots.Add(node);
        return node;
    }

    /// <summary>
    /// Expands the tree down to <paramref name="folder"/> (or its deepest visible ancestor, if some
    /// level is hidden or missing) and returns that node. Each level is listed in the background.
    /// Returns null if cancelled.
    /// </summary>
    public async Task<FolderNode?> RevealAsync(string folder, CancellationToken ct)
    {
        folder = PathUtil.NormalizeFolder(folder);
        var node = FindOrAddRoot(folder);
        if (node == null) return null;

        string rest = folder.Length > node.Path.Length ? folder.Substring(node.Path.Length) : "";
        foreach (var segment in rest.Split('\\', StringSplitOptions.RemoveEmptyEntries))
        {
            await node.LoadChildrenAsync();
            if (ct.IsCancellationRequested) return null;
            var child = node.FindChild(segment);
            if (child == null) break;
            node.IsExpanded = true;
            node = child;
        }
        return ct.IsCancellationRequested ? null : node;
    }

    // FOLDERID_Downloads has no Environment.SpecialFolder value.
    private static string? GetDownloadsFolder()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            var id = new Guid("374DE290-123F-4565-9164-39C4925E467B");
            if (SHGetKnownFolderPath(id, 0, IntPtr.Zero, out IntPtr p) != 0) return null;
            try { return Marshal.PtrToStringUni(p); }
            finally { Marshal.FreeCoTaskMem(p); }
        }
        catch
        {
            return null;
        }
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint flags,
        IntPtr token, out IntPtr path);
}

internal static class PathUtil
{
    /// <summary>Full path without trailing separator, except for roots ("C:\", "\\server\share").</summary>
    public static string NormalizeFolder(string path)
    {
        string full = Path.GetFullPath(path.Trim().Trim('"'));
        string? root = Path.GetPathRoot(full);
        if (full.Length > 3 && !string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
            full = full.TrimEnd('\\', '/');
        return full;
    }

    public static bool IsSameOrInside(string path, string folder)
    {
        if (string.Equals(path, folder, StringComparison.OrdinalIgnoreCase)) return true;
        string prefix = folder.EndsWith('\\') ? folder : folder + "\\";
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>"\\server\share" for a UNC path, otherwise null.</summary>
    public static string? GetUncShareRoot(string path)
    {
        if (!path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith(@"\\?\", StringComparison.Ordinal))
            return null;
        var parts = path.Substring(2).Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? $@"\\{parts[0]}\{parts[1]}" : null;
    }
}
