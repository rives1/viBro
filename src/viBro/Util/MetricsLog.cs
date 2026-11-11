using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ViBro.Util;

internal sealed record MetricRecord(
    DateTime TimestampUtc,
    string Kind,
    double Ms,
    string FileName,
    long FileSize,
    string Storage,
    string SeekMode,
    double? Delta,
    double? Target,
    double? Landed,
    int Coalesced,
    string? Codec,
    long Width,
    long Height,
    string? Hwdec,
    string? Vo,
    double WorkingSetMb,
    double PrivateMb,
    double ManagedMb,
    string Version)
{
    public const string Header =
        "timestamp_utc,kind,ms,file,size_bytes,storage,seek_mode,delta_s,target_s,landed_s,coalesced," +
        "codec,width,height,hwdec,vo,working_set_mb,private_mb,managed_mb,version";

    public string ToCsv()
    {
        var c = CultureInfo.InvariantCulture;
        return string.Join(",",
            TimestampUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", c),
            Kind,
            Ms.ToString("0.0", c),
            Quote(FileName),
            FileSize.ToString(c),
            Storage,
            SeekMode,
            Delta?.ToString("0.###", c) ?? "",
            Target?.ToString("0.###", c) ?? "",
            Landed?.ToString("0.###", c) ?? "",
            Coalesced.ToString(c),
            Quote(Codec ?? ""),
            Width.ToString(c),
            Height.ToString(c),
            Quote(Hwdec ?? ""),
            Quote(Vo ?? ""),
            WorkingSetMb.ToString("0.0", c),
            PrivateMb.ToString("0.0", c),
            ManagedMb.ToString("0.0", c),
            Version);
    }

    private static string Quote(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
}

/// <summary>
/// Appends measurements to viBro-metrics.csv next to the executable.
/// Only file names are written (no folders), so results can be shared safely.
/// </summary>
internal sealed class MetricsLog
{
    private readonly string _path;
    private bool _failed;

    public MetricsLog(string path) => _path = path;

    public string Path => _path;
    public string? LastError { get; private set; }

    public static (double WorkingSetMb, double PrivateMb, double ManagedMb) MemorySnapshot()
    {
        using var p = Process.GetCurrentProcess();
        const double mb = 1024.0 * 1024.0;
        return (p.WorkingSet64 / mb, p.PrivateMemorySize64 / mb, GC.GetTotalMemory(false) / mb);
    }

    private bool HeaderMatches()
    {
        using var r = new StreamReader(_path);
        return r.ReadLine() == MetricRecord.Header;
    }

    private const long MaxBytes = 10L * 1024 * 1024; // 1.12.0: beyond this the file becomes viBro-metrics-old.csv
    private bool _headerChecked;

    public void Append(MetricRecord record)
    {
        if (_failed) return;
        try
        {
            bool isNew = !File.Exists(_path);
            if (!isNew && !_headerChecked && !HeaderMatches())
            {
                // Column layout changed: keep the old file aside instead of mixing layouts.
                File.Move(_path, System.IO.Path.ChangeExtension(_path, null) + "-" +
                    DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".csv");
                isNew = true;
            }
            _headerChecked = true; // checked once per session, not at every record
            if (!isNew && new FileInfo(_path).Length >= MaxBytes)
            {
                File.Move(_path, System.IO.Path.ChangeExtension(_path, null) + "-old.csv", overwrite: true);
                isNew = true;
            }
            using var w = new StreamWriter(_path, append: true, new UTF8Encoding(false));
            if (isNew) w.WriteLine(MetricRecord.Header);
            w.WriteLine(record.ToCsv());
        }
        catch (Exception ex)
        {
            _failed = true;
            LastError = ex.Message;
        }
    }
}

/// <summary>Small rolling sample for the status bar (median of the last N values).</summary>
internal sealed class RollingStats
{
    private readonly Queue<double> _values = new();
    private readonly int _capacity;

    public RollingStats(int capacity) => _capacity = capacity;

    public int Count { get; private set; }

    public void Add(double v)
    {
        _values.Enqueue(v);
        if (_values.Count > _capacity) _values.Dequeue();
        Count++;
    }

    public double Median
    {
        get
        {
            if (_values.Count == 0) return 0;
            var s = _values.OrderBy(x => x).ToArray();
            int n = s.Length;
            return n % 2 == 1 ? s[n / 2] : (s[n / 2 - 1] + s[n / 2]) / 2;
        }
    }
}
