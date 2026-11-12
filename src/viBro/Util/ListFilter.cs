using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ViBro.Util;

/// <summary>
/// The file list filter (1.15.0), parsed from the text of the filter box. Words separated by spaces,
/// all required (AND):
/// <list type="bullet">
/// <item><c>4K</c>: the name contains it (case-insensitive);</item>
/// <item><c>.mp4</c>: the extension; several extensions are alternatives (<c>.mp4 .mkv</c> = MP4 or MKV);</item>
/// <item><c>&gt;1000</c>, <c>&lt;500M</c>, <c>&gt;=1.5G</c>: the size; a bare number is in MB, units K, M, G, T
/// (optional B), in multiples of 1024 like the Size column.</item>
/// </list>
/// Anything else, including a size that cannot be read, is a word. Only videos are filtered: folders and
/// ".." always stay. Pure code, no WPF: works on the items already in memory.
/// </summary>
internal sealed class ListFilter
{
    public static readonly ListFilter None = new(Array.Empty<string>(), Array.Empty<string>(), null, null, "");

    private static readonly Regex SizeToken = new(@"^(?<op>[<>]=?)(?<num>\d+(?:[.,]\d+)?)(?<unit>[KMGT]?)B?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private ListFilter(string[] words, string[] extensions, long? minBytes, long? maxBytes, string text)
    {
        Words = words;
        Extensions = extensions;
        MinBytes = minBytes;
        MaxBytes = maxBytes;
        Text = text;
    }

    /// <summary>Name parts, as typed (for the highlighting).</summary>
    public IReadOnlyList<string> Words { get; }

    /// <summary>Extensions with the dot, any of them.</summary>
    public IReadOnlyList<string> Extensions { get; }

    /// <summary>Size limits in bytes: &gt; and &gt;= give a minimum, &lt; and &lt;= a maximum (both inclusive after rounding to the operator).</summary>
    public long? MinBytes { get; }
    public long? MaxBytes { get; }

    /// <summary>The text the filter was made from, trimmed.</summary>
    public string Text { get; }

    public bool IsEmpty => Words.Count == 0 && Extensions.Count == 0 && MinBytes == null && MaxBytes == null;

    public static ListFilter Parse(string? text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return None;
        var words = new List<string>();
        var extensions = new List<string>();
        long? min = null, max = null;
        foreach (string token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseSize(token, out bool greater, out long bytes))
            {
                if (greater) min = min == null ? bytes : Math.Max(min.Value, bytes);
                else max = max == null ? bytes : Math.Min(max.Value, bytes);
            }
            else if (token.Length > 1 && token[0] == '.' && token.IndexOf('.', 1) < 0)
            {
                if (!extensions.Contains(token, StringComparer.OrdinalIgnoreCase)) extensions.Add(token);
            }
            else words.Add(token);
        }
        return new ListFilter(words.ToArray(), extensions.ToArray(), min, max, text);
    }

    /// <summary>"&gt;1000" → greater, 1000 MB in bytes (strict "&gt;" becomes a minimum of bytes + 1).</summary>
    private static bool TryParseSize(string token, out bool greater, out long bytes)
    {
        greater = false;
        bytes = 0;
        var m = SizeToken.Match(token);
        if (!m.Success) return false;
        if (!double.TryParse(m.Groups["num"].Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out double number)) return false;
        double factor = m.Groups["unit"].Value.ToUpperInvariant() switch
        {
            "K" => 1024d,
            "G" => 1024d * 1024 * 1024,
            "T" => 1024d * 1024 * 1024 * 1024,
            _ => 1024d * 1024, // bare number or M: megabytes
        };
        double value = number * factor;
        if (value > long.MaxValue / 2) value = long.MaxValue / 2;
        string op = m.Groups["op"].Value;
        greater = op[0] == '>';
        bool strict = op.Length == 1;
        long b = (long)Math.Round(value);
        bytes = !strict ? b : greater ? b + 1 : Math.Max(0, b - 1);
        return true;
    }

    /// <summary>True when the video passes every condition. Callers keep folders and ".." regardless.</summary>
    public bool Matches(string name, long size)
    {
        if (MinBytes is long min && size < min) return false;
        if (MaxBytes is long max && size > max) return false;
        if (Extensions.Count > 0)
        {
            string ext = Path.GetExtension(name);
            bool any = false;
            foreach (string e in Extensions)
                if (string.Equals(ext, e, StringComparison.OrdinalIgnoreCase)) { any = true; break; }
            if (!any) return false;
        }
        foreach (string w in Words)
            if (name.IndexOf(w, StringComparison.OrdinalIgnoreCase) < 0) return false;
        return true;
    }

    /// <summary>
    /// The parts of <paramref name="name"/> matched by the words, as (start, length) in order, overlaps
    /// merged: what the list highlights.
    /// </summary>
    public static List<(int Start, int Length)> MatchRanges(string name, IReadOnlyList<string> words)
    {
        var ranges = new List<(int Start, int End)>();
        foreach (string w in words)
        {
            if (w.Length == 0) continue;
            int at = 0;
            while (at < name.Length && (at = name.IndexOf(w, at, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                ranges.Add((at, at + w.Length));
                at += w.Length;
            }
        }
        ranges.Sort((a, b) => a.Start.CompareTo(b.Start));
        var merged = new List<(int Start, int Length)>();
        int s = -1, e = -1;
        foreach (var r in ranges)
        {
            if (s < 0) { s = r.Start; e = r.End; continue; }
            if (r.Start <= e) { e = Math.Max(e, r.End); continue; }
            merged.Add((s, e - s));
            s = r.Start;
            e = r.End;
        }
        if (s >= 0) merged.Add((s, e - s));
        return merged;
    }
}
