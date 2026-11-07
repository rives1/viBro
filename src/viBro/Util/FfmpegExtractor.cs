using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViBro.Util;

/// <summary>
/// Extract of a part of a video (I / O / F9, 1.5.0) with an ffmpeg.exe supplied by the user next to
/// viBro.exe. Stream copy (no re-encoding): fast and lossless, but the extract starts at the keyframe
/// at or before the start marker. Without ffmpeg.exe the feature is disabled.
/// </summary>
internal static class FfmpegExtractor
{
    public const string ExeName = "ffmpeg.exe";

    public static string ExePath => Path.Combine(AppInfo.ExeDirectory, ExeName);

    /// <summary>Checked at every use, so ffmpeg.exe can be added or removed without restarting viBro.</summary>
    public static bool IsAvailable => File.Exists(ExePath);

    /// <summary>"name.mp4" → "name-extract-261104-1445.mp4"; "-2", "-3" ... if that name already exists.</summary>
    public static string OutputName(string folder, string sourceName, DateTime now)
    {
        string stem = Path.GetFileNameWithoutExtension(sourceName);
        string ext = Path.GetExtension(sourceName);
        string baseName = stem + "-extract-" + now.ToString("yyMMdd-HHmm", CultureInfo.InvariantCulture);
        string name = baseName + ext;
        for (int i = 2; File.Exists(Path.Combine(folder, name)) || Directory.Exists(Path.Combine(folder, name)); i++)
            name = $"{baseName}-{i}{ext}";
        return name;
    }

    /// <summary>Command-line arguments (exposed for documentation and tests).</summary>
    public static List<string> BuildArguments(string source, string target, double start, double end)
    {
        var c = CultureInfo.InvariantCulture;
        bool mkv = string.Equals(Path.GetExtension(source), ".mkv", StringComparison.OrdinalIgnoreCase);
        var a = new List<string>
        {
            "-hide_banner", "-nostdin", "-nostats", "-loglevel", "error",
            "-progress", "pipe:1",
            // Input seeking: fast also on a NAS; with stream copy it starts at the previous keyframe.
            "-ss", start.ToString("0.000", c),
            "-i", source,
            "-t", (end - start).ToString("0.000", c),
            "-map", "0:v", "-map", "0:a?",
        };
        // Subtitles only in MKV (any codec fits); MP4/MOV may hold subtitle or data tracks that cannot be copied.
        if (mkv) a.AddRange(new[] { "-map", "0:s?" });
        // .m4v: MP4 container, said explicitly (ffmpeg also has a raw "m4v" muxer for that extension).
        if (string.Equals(Path.GetExtension(target), ".m4v", StringComparison.OrdinalIgnoreCase))
            a.AddRange(new[] { "-f", "mp4" });
        a.AddRange(new[]
        {
            "-c", "copy",
            "-avoid_negative_ts", "make_zero",
            "-n", // never overwrite (the name is unique, this is a safety net)
            target,
        });
        return a;
    }

    /// <summary>
    /// Runs ffmpeg and waits for it. Progress 0..1 from "-progress". On error or cancellation the
    /// partial output is deleted and the exception is rethrown. Returns the final path.
    /// </summary>
    public static async Task<string> ExtractAsync(string source, string target, double start, double end,
        IProgress<double>? progress, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(ExePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = AppInfo.ExeDirectory,
        };
        foreach (string arg in BuildArguments(source, target, start, end)) psi.ArgumentList.Add(arg);

        double length = Math.Max(end - start, 0.001);
        var errors = new List<string>();
        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (_, e) =>
        {
            // "out_time_us=12345678" (microseconds written so far)
            if (e.Data == null || !e.Data.StartsWith("out_time_us=", StringComparison.Ordinal)) return;
            if (long.TryParse(e.Data.AsSpan("out_time_us=".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out long us) && us >= 0)
                progress?.Report(Math.Clamp(us / 1e6 / length, 0, 1));
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data)) return;
            lock (errors)
            {
                errors.Add(e.Data.Trim());
                if (errors.Count > 20) errors.RemoveAt(0);
            }
        };

        // Only a file created by this run may be deleted on failure.
        if (File.Exists(target)) throw new IOException("A file with the extract's name already exists: " + Path.GetFileName(target));
        bool started = false;
        try
        {
            if (!process.Start()) throw new IOException("ffmpeg.exe could not be started.");
            started = true;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
                await process.WaitForExitAsync(CancellationToken.None);
                throw;
            }
            process.WaitForExit(); // flushes the redirected streams

            if (process.ExitCode != 0)
            {
                string detail;
                lock (errors) detail = errors.Count > 0 ? string.Join(Environment.NewLine, errors.GetRange(Math.Max(0, errors.Count - 5), Math.Min(5, errors.Count))) : "";
                throw new IOException($"ffmpeg ended with code {process.ExitCode}." + (detail.Length > 0 ? Environment.NewLine + detail : ""));
            }
            if (!File.Exists(target) || new FileInfo(target).Length == 0)
                throw new IOException("ffmpeg did not write the extract (empty or missing file).");
            progress?.Report(1);
            return target;
        }
        catch
        {
            if (started) TryDelete(target); // never leave a partial file behind
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            ErrorLog.Write("extract", "Partial extract not deleted: " + path, ex);
        }
    }
}
