using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ViBro.Util;

/// <summary>
/// viBro-errors.log next to the EXE: unexpected exceptions, playback errors from mpv and failed
/// file operations. Size-limited (1 MB, one previous file kept as .1.log). Never throws; if the
/// folder is not writable the log is silently disabled (the status bar still shows the errors).
/// </summary>
internal static class ErrorLog
{
    private const long MaxBytes = 1024 * 1024;
    private static readonly object Gate = new();
    private static bool _disabled;

    public static string FilePath { get; } = Path.Combine(AppInfo.ExeDirectory, "viBro-errors.log");

    public static void Write(string category, string message, Exception? ex = null)
    {
        lock (Gate)
        {
            if (_disabled) return;
            try
            {
                RotateIfNeeded();
                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
                  .Append(" [").Append(category).Append("] ")
                  .Append(AppInfo.SemanticVersion).Append(" | ")
                  .Append(message.Replace("\r", " ").Replace("\n", " "));
                if (ex != null) sb.Append("\r\n    ").Append(ex.ToString().Replace("\n", "\n    "));
                sb.Append("\r\n");
                File.AppendAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
                _disabled = true;
            }
        }
    }

    private static void RotateIfNeeded()
    {
        var fi = new FileInfo(FilePath);
        if (!fi.Exists || fi.Length < MaxBytes) return;
        string old = Path.Combine(AppInfo.ExeDirectory, "viBro-errors.1.log");
        File.Move(FilePath, old, overwrite: true);
    }
}
