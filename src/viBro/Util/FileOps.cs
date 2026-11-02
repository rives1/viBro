using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ViBro.Util;

internal enum ConflictChoice { Cancel, KeepBoth, Overwrite }

internal enum DeleteOutcome { Deleted, NotDeleted }

/// <summary>
/// File operations used by delete, rename and move. Everything here reports what really
/// happened on disk; callers update the UI from these results only.
/// </summary>
internal static class FileOps
{
    private static readonly string[] ReservedNames =
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    // ------------------------------------------------------------------ classification

    /// <summary>UNC paths and mapped network drives: no Recycle Bin, deletion is permanent.</summary>
    public static bool IsNetworkPath(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal)) return true;
        try
        {
            string? root = Path.GetPathRoot(path);
            return !string.IsNullOrEmpty(root) && new DriveInfo(root).DriveType == DriveType.Network;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsSameVolume(string a, string b)
        => string.Equals(Path.GetPathRoot(a), Path.GetPathRoot(b), StringComparison.OrdinalIgnoreCase);

    public static bool IsSameFolder(string a, string b)
        => string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)),
                         Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
                         StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------------ release

    /// <summary>
    /// Waits until no other handle keeps the file open (the player closes it asynchronously).
    /// Returns true when the file can be opened exclusively or no longer exists.
    /// </summary>
    public static async Task<bool> WaitUntilReleasedAsync(string path, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (true)
        {
            bool free = await Task.Run(() =>
            {
                try
                {
                    using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                    return true;
                }
                catch (FileNotFoundException) { return true; }
                catch (DirectoryNotFoundException) { return true; }
                catch (UnauthorizedAccessException) { return true; } // not a lock: let the operation report it
                catch (IOException) { return false; }                // sharing violation: still open
            });
            if (free) return true;
            if (sw.Elapsed > timeout) return false;
            await Task.Delay(100);
        }
    }

    // ------------------------------------------------------------------ rename

    /// <summary>Returns an error message for an invalid new base name (without extension), or null.</summary>
    public static string? ValidateBaseName(string baseName, string extension)
    {
        if (string.IsNullOrWhiteSpace(baseName)) return "The name cannot be empty.";
        if (baseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || baseName.IndexOfAny(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' }) >= 0)
            return "A file name cannot contain any of these characters: \\ / : * ? \" < > |";
        if (baseName != baseName.TrimStart())
            return "The name cannot start with a space.";
        string full = baseName + extension;
        if (full.EndsWith(' ') || full.EndsWith('.'))
            return "The name cannot end with a space or a dot.";
        string stem = full.Split('.')[0].TrimEnd();
        if (ReservedNames.Any(r => string.Equals(r, stem, StringComparison.OrdinalIgnoreCase)))
            return $"\"{stem}\" is a reserved name in Windows.";
        if (full.Length > 255)
            return "The name is too long (maximum 255 characters).";
        return null;
    }

    /// <summary>
    /// Renames a file inside its folder, never overwriting another file.
    /// A change of letter case only is allowed. Returns the new full path.
    /// </summary>
    public static string Rename(string sourcePath, string newFileName)
    {
        string dir = Path.GetDirectoryName(sourcePath) ?? throw new IOException("Invalid path.");
        string target = Path.Combine(dir, newFileName);
        bool caseOnly = string.Equals(sourcePath, target, StringComparison.OrdinalIgnoreCase);
        if (!caseOnly && (File.Exists(target) || Directory.Exists(target)))
            throw new IOException($"\"{newFileName}\" already exists in this folder.");
        File.Move(sourcePath, target, overwrite: false);
        return target;
    }

    // ------------------------------------------------------------------ delete

    /// <summary>Permanent deletion (network files). The result is checked on disk.</summary>
    public static DeleteOutcome DeletePermanently(string path)
    {
        File.Delete(path);
        return File.Exists(path) ? DeleteOutcome.NotDeleted : DeleteOutcome.Deleted;
    }

    /// <summary>
    /// Sends a local file to the Recycle Bin. If Windows cannot recycle it (file too large,
    /// drive without a Recycle Bin), Windows itself asks before deleting it permanently
    /// (FOF_WANTNUKEWARNING); declining leaves the file in place. Must run on the UI thread.
    /// </summary>
    public static DeleteOutcome Recycle(string path, IntPtr ownerHwnd, out string? error)
    {
        error = null;
        var op = new SHFILEOPSTRUCT
        {
            hwnd = ownerHwnd,
            wFunc = FO_DELETE,
            pFrom = Path.GetFullPath(path) + "\0", // double-null-terminated list
            pTo = null,
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT | FOF_WANTNUKEWARNING,
        };
        int rc = SHFileOperation(ref op);
        if (rc != 0 && !op.fAnyOperationsAborted) error = $"Windows error 0x{rc:X}";
        return File.Exists(path) ? DeleteOutcome.NotDeleted : DeleteOutcome.Deleted;
    }

    // ------------------------------------------------------------------ move

    /// <summary>"name (2).ext", "name (3).ext"... for the first name not used in the folder.</summary>
    public static string UniqueName(string folder, string fileName)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName);
        string ext = Path.GetExtension(fileName);
        for (int i = 2; ; i++)
        {
            string candidate = $"{stem} ({i}){ext}";
            if (!File.Exists(Path.Combine(folder, candidate)) && !Directory.Exists(Path.Combine(folder, candidate)))
                return candidate;
        }
    }

    /// <summary>
    /// Moves a file into <paramref name="destFolder"/> under <paramref name="destName"/>.
    /// Same volume: a rename (instant). Otherwise: copy to a temporary file with progress and
    /// cancellation, check the size, give it its final name, then delete the source. The source
    /// is untouched until the copy is complete. Returns the final path.
    /// </summary>
    public static async Task<string> MoveAsync(string source, string destFolder, string destName, bool overwrite,
        IProgress<double>? progress, CancellationToken ct, bool forceCopy = false)
    {
        string target = Path.Combine(destFolder, destName);
        if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Source and destination are the same file.");

        if (!forceCopy && IsSameVolume(source, target))
        {
            await Task.Run(() => File.Move(source, target, overwrite), ct);
            progress?.Report(1);
            return target;
        }

        string temp = target + ".vibro-partial";
        await Task.Run(() => CopyWithProgress(source, temp, progress, ct), ct).ConfigureAwait(false);

        try
        {
            ct.ThrowIfCancellationRequested();
            File.Move(temp, target, overwrite); // fails if a file appeared meanwhile and overwrite is false
        }
        catch
        {
            TryDelete(temp);
            throw;
        }

        // Copy complete and in place: only now remove the source.
        try
        {
            File.Delete(source);
        }
        catch (Exception ex)
        {
            throw new SourceNotDeletedException(target, ex);
        }
        return target;
    }

    private static void CopyWithProgress(string source, string temp, IProgress<double>? progress, CancellationToken ct)
    {
        const int bufferSize = 4 * 1024 * 1024;
        var info = new FileInfo(source);
        long total = info.Length, done = 0;
        var lastReport = Stopwatch.StartNew();
        try
        {
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
            using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize))
            {
                output.SetLength(total); // pre-allocate; also fails early when the disk is full
                var buffer = new byte[bufferSize];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    output.Write(buffer, 0, read);
                    done += read;
                    if (lastReport.ElapsedMilliseconds >= 100)
                    {
                        progress?.Report(total == 0 ? 1 : (double)done / total);
                        lastReport.Restart();
                    }
                }
                output.Flush(flushToDisk: true);
            }

            if (new FileInfo(temp).Length != total || done != total)
                throw new IOException("The copy is incomplete (size mismatch); the source was not deleted.");

            File.SetCreationTimeUtc(temp, info.CreationTimeUtc);
            File.SetLastWriteTimeUtc(temp, info.LastWriteTimeUtc);
            progress?.Report(1);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }

    // ------------------------------------------------------------------ Win32

    private const uint FO_DELETE = 3;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOERRORUI = 0x0400;
    private const ushort FOF_WANTNUKEWARNING = 0x4000;

    // x64 layout (natural alignment); viBro is x64 only.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT op);
}

/// <summary>The file was copied to the destination, but the source could not be removed.</summary>
internal sealed class SourceNotDeletedException : IOException
{
    public SourceNotDeletedException(string copiedTo, Exception inner)
        : base($"The file was copied to {copiedTo}, but the original could not be deleted: {inner.Message}", inner)
        => CopiedTo = copiedTo;

    public string CopiedTo { get; }
}
