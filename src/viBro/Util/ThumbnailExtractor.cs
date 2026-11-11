using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
    // "[Parsed_showinfo_5 @ 0x...] n:   0 pts: ... pts_time:120 ..." (first frame of each chain).
    private static readonly Regex StripPtsRx = new(@"Parsed_showinfo_(\d+)\s*@[^\]]*\]\s*n:\s*0\s[^\r\n]*?pts_time:\s*(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant);

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

    /// <summary>
    /// 1.12.0, one ffmpeg run for all the images of a video: one input per position (each seeks to its
    /// keyframe), one frame per chain (trim), timestamps reset (setpts=0, otherwise hstack would wait
    /// until all inputs reach the same time, decoding the whole file), then the images side by side in
    /// one JPEG strip. showinfo comes before trim, so its pts_time is still the frame's real time.
    /// </summary>
    public static List<string> StripArguments(string source, IReadOnlyList<double> seconds)
    {
        var c = CultureInfo.InvariantCulture;
        var a = new List<string> { "-hide_banner", "-nostdin", "-loglevel", "info" };
        foreach (double t in seconds)
            a.AddRange(new[] { "-noaccurate_seek", "-ss", t.ToString("0.000", c), "-i", source });
        var graph = new System.Text.StringBuilder();
        for (int i = 0; i < seconds.Count; i++)
            graph.Append($"[{i}:v:0]showinfo,trim=end_frame=1,setpts=0,scale=w={BoxWidth}:h={BoxHeight}:force_original_aspect_ratio=decrease,setsar=1[v{i}];");
        for (int i = 0; i < seconds.Count; i++) graph.Append($"[v{i}]");
        graph.Append($"hstack=inputs={seconds.Count}[strip]");
        a.AddRange(new[]
        {
            "-copyts", "-filter_complex", graph.ToString(), "-map", "[strip]", "-frames:v", "1",
            "-f", "image2pipe", "-c:v", "mjpeg", "-q:v", "4", "pipe:1",
        });
        return a;
    }

    /// <summary>Real times of the strip's images, in input order; null unless there is exactly one per input.</summary>
    public static double[]? ParseStripTimes(string stderr, int count)
    {
        var byFilter = new SortedDictionary<int, double>();
        foreach (Match m in StripPtsRx.Matches(stderr))
        {
            int filter = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            byFilter.TryAdd(filter, double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
        }
        return byFilter.Count == count ? byFilter.Values.ToArray() : null; // filters are numbered in chain order
    }

    /// <summary>The strip (JPEG, images side by side, same width each) and the real time of every image.</summary>
    public static async Task<(byte[] Jpeg, double[] Seconds)> GrabStripAsync(string source, IReadOnlyList<double> seconds,
        double start, CancellationToken ct)
    {
        var (stdout, stderr, code) = await RunAsync(StripArguments(source, seconds), ct);
        if (code != 0 || stdout.Length == 0)
            throw new IOException(LastLine(stderr) ?? $"ffmpeg ended with code {code}");
        double[]? frames = ParseStripTimes(stderr, seconds.Count);
        return (stdout, frames == null
            ? seconds.ToArray() // times not readable: the requested positions (already relative to the start)
            : frames.Select(f => Math.Max(0, f - start)).ToArray());
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
        var a = new List<string> { "-hide_banner", "-nostdin", "-loglevel", "info" };
        a.AddRange(new List<string>
        {
            "-noaccurate_seek", "-ss", seconds.ToString("0.000", c),
            "-i", source,
            "-copyts", "-map", "0:v:0", "-frames:v", "1", "-an", "-sn", "-dn",
            "-vf", $"showinfo,scale=w={BoxWidth}:h={BoxHeight}:force_original_aspect_ratio=decrease",
            "-f", "image2pipe", "-c:v", "mjpeg", "-q:v", "4", "pipe:1",
        });
        return a;
    }

    public static async Task<ProbeResult> ProbeAsync(string source, CancellationToken ct)
    {
        // Without an output ffmpeg ends with code 1 after printing the input's details: expected.
        var (_, stderr, _) = await RunAsync(ProbeArguments(source), ct);
        return ParseProbe(stderr) ?? throw new IOException(LastLine(stderr) ?? "duration unknown");
    }

    /// <summary>Earlier positions tried when no image comes (1.12.0): see <see cref="GrabNearAsync"/>.</summary>
    private static readonly double[] RetryEarlierBy = { 10, 30 };

    /// <summary>
    /// The image at <paramref name="seconds"/>; if none comes, the same 10 s and then 30 s earlier. In
    /// MPEG-TS (and some other formats) ffmpeg seeks to the keyframe after the position, so near the
    /// end (e.g. 99 %) there may be none: an earlier position finds the last keyframe. The time of the
    /// image is always the real one.
    /// </summary>
    public static async Task<Thumbnail> GrabNearAsync(string source, double seconds, double start, CancellationToken ct)
    {
        try
        {
            return await GrabAsync(source, seconds, start, ct);
        }
        catch (Exception first) when (first is not OperationCanceledException)
        {
            foreach (double back in RetryEarlierBy)
            {
                if (seconds - back < 0) break;
                try
                {
                    return await GrabAsync(source, seconds - back, start, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // try further back
                }
            }
            throw;
        }
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
