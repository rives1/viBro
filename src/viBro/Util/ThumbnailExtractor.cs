using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ViBro.Util;

/// <summary>Duration and start time of a file, as ffmpeg reports them.</summary>
internal sealed record ProbeResult(double Duration, double Start);

/// <summary>One gallery image: JPEG bytes and the time of the frame actually taken (keyframe).</summary>
internal sealed record Thumbnail(byte[] Jpeg, double Seconds);

/// <summary>
/// Gallery images (F10, 1.6.0) with the same ffmpeg.exe used for the extract (FfmpegExtractor).
/// One ffmpeg run reads the duration, then one run per image: input seeking without accurate
/// seek, so the image is the keyframe at or near the requested time (fast, also on a NAS) and
/// its real time is read from the showinfo filter. Nothing is written to disk.
/// </summary>
internal static class ThumbnailExtractor
{
    /// <summary>The image fits in this box, keeping its proportions.</summary>
    public const int BoxWidth = 480, BoxHeight = 270;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30); // a share that stops answering
    private static readonly Regex DurationRx = new(@"Duration:\s*(\d+):(\d{2}):(\d{2}(?:\.\d+)?)", RegexOptions.CultureInvariant);
    private static readonly Regex StartRx = new(@"Duration:[^\r\n]*?start:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant);
    private static readonly Regex PtsRx = new(@"pts_time:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant);

    /// <summary>Times (seconds) for the given percentages of the duration, kept inside the video.</summary>
    public static double[] Positions(double duration, IReadOnlyList<double> percents)
    {
        var t = new double[percents.Count];
        double last = Math.Max(0, duration - 0.5); // a seek to the very end may find no frame
        for (int i = 0; i < t.Length; i++) t[i] = Math.Clamp(duration * percents[i] / 100.0, 0, last);
        return t;
    }

    /// <summary>Parses "Duration: 00:01:23.45, start: 0.000000" from ffmpeg's banner; null if unknown (N/A).</summary>
    public static ProbeResult? ParseProbe(string stderr)
    {
        var c = CultureInfo.InvariantCulture;
        var m = DurationRx.Match(stderr);
        if (!m.Success) return null;
        double duration = int.Parse(m.Groups[1].Value, c) * 3600 + int.Parse(m.Groups[2].Value, c) * 60
                        + double.Parse(m.Groups[3].Value, c);
        if (duration <= 0) return null;
        var s = StartRx.Match(stderr);
        double start = s.Success ? double.Parse(s.Groups[1].Value, c) : 0;
        return new ProbeResult(duration, start);
    }

    /// <summary>First "pts_time:" of the showinfo filter; null if absent.</summary>
    public static double? ParseFrameTime(string stderr)
    {
        var m = PtsRx.Match(stderr);
        return m.Success ? double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : null;
    }

    public static List<string> ProbeArguments(string source) =>
        new() { "-hide_banner", "-nostdin", "-i", source };

    /// <summary>
    /// -copyts keeps the file's own timestamps, so showinfo prints the absolute time of the frame;
    /// minus the start time it is the position mpv shows (mpv starts every file at 0).
    /// </summary>
    public static List<string> ThumbnailArguments(string source, double seconds)
    {
        var c = CultureInfo.InvariantCulture;
        return new()
        {
            "-hide_banner", "-nostdin", "-loglevel", "info",
            "-noaccurate_seek", "-ss", seconds.ToString("0.000", c),
            "-i", source,
            "-copyts", "-map", "0:v:0", "-frames:v", "1", "-an", "-sn", "-dn",
            "-vf", $"showinfo,scale=w={BoxWidth}:h={BoxHeight}:force_original_aspect_ratio=decrease",
            "-f", "image2pipe", "-c:v", "mjpeg", "-q:v", "4", "pipe:1",
        };
    }

    public static async Task<ProbeResult> ProbeAsync(string source, CancellationToken ct)
    {
        // Without an output ffmpeg ends with code 1 after printing the input's details: expected.
        var (_, stderr, _) = await RunAsync(ProbeArguments(source), ct);
        return ParseProbe(stderr) ?? throw new IOException(LastLine(stderr) ?? "duration unknown");
    }

    public static async Task<Thumbnail> GrabAsync(string source, double seconds, double start, CancellationToken ct)
    {
        var (stdout, stderr, code) = await RunAsync(ThumbnailArguments(source, seconds), ct);
        if (code != 0 || stdout.Length == 0)
            throw new IOException(LastLine(stderr) ?? $"ffmpeg ended with code {code}");
        double frame = ParseFrameTime(stderr) is double pts ? pts - start : seconds;
        return new Thumbnail(stdout, Math.Max(0, frame));
    }

    private static string? LastLine(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Length > 0 ? lines[^1] : null;
    }

    /// <summary>Runs ffmpeg.exe; stdout as bytes, stderr as text. Killed on cancellation or after 30 s.</summary>
    private static async Task<(byte[] Stdout, string Stderr, int ExitCode)> RunAsync(List<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(FfmpegExtractor.ExePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = AppInfo.ExeDirectory,
        };
        foreach (string a in args) psi.ArgumentList.Add(a);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);
        using var process = new Process { StartInfo = psi };
        if (!process.Start()) throw new IOException("ffmpeg.exe could not be started.");
        var output = new MemoryStream();
        Task copy = process.StandardOutput.BaseStream.CopyToAsync(output, CancellationToken.None);
        Task<string> errors = process.StandardError.ReadToEndAsync(CancellationToken.None);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
            await process.WaitForExitAsync(CancellationToken.None);
            ct.ThrowIfCancellationRequested();
            throw new TimeoutException("ffmpeg did not answer within 30 s");
        }
        await Task.WhenAll(copy, errors);
        return (output.ToArray(), errors.Result, process.ExitCode);
    }
}
