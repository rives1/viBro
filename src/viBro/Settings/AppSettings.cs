using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ViBro.Settings;

internal enum SortColumn { Name, Size, Date }

/// <summary>System = follow the Windows light/dark setting (only until the user picks a theme).</summary>
internal enum ThemeChoice { System, Light, Dark }

/// <summary>
/// Typed view of viBro.ini (next to the EXE). Missing or invalid values fall back to
/// defaults (each invalid value is reported once). Unknown keys are preserved on save.
/// </summary>
internal sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;
    public const string FileName = "viBro.ini";

    /// <summary>Allowed range of the [Seek] steps (0.1 s = the INI precision; 1 h).</summary>
    public const double MinSeekSeconds = 0.1, MaxSeekSeconds = 3600;

    /// <summary>"1 s", "1.5 s", "1 min", "90 s": a whole number of minutes is shown in minutes.</summary>
    public static string FormatSeconds(double seconds)
    {
        var c = CultureInfo.InvariantCulture;
        if (seconds >= 60 && seconds % 60 == 0) return (seconds / 60).ToString("0", c) + " min";
        return seconds.ToString("0.#", c) + " s";
    }

    private IniFile _ini = new();

    // [Appearance]
    public ThemeChoice Theme { get; set; } = ThemeChoice.System;

    // [Window]
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 820;
    public bool WindowMaximized { get; set; }

    // [Layout]
    public double TreeWidth { get; set; } = 280;
    public double ListHeight { get; set; } = 260;

    // [List]
    public SortColumn SortColumn { get; set; } = SortColumn.Name;
    public bool SortDescending { get; set; }
    public double NameColumnWidth { get; set; } = 480;
    public double SizeColumnWidth { get; set; } = 100;
    public double DateColumnWidth { get; set; } = 150;

    // [Folders]
    public string? LastFolder { get; set; }
    public string? LastMoveDestination { get; set; }
    public string? LastCopyDestination { get; set; }

    // [Audio]
    public double Volume { get; set; } = 100;
    public bool Mute { get; set; }

    // [Playback]
    public string HardwareDecoding { get; set; } = "auto-safe";
    public bool ExactLongSeeks { get; set; }
    public double ExactSeekThresholdSeconds { get; set; } = 30;

    // [Seek] step of each seek command, in seconds (1.4.0)
    public double SeekLeftRightSeconds { get; set; } = 1;   // Left / Right arrow
    public double SeekUpDownSeconds { get; set; } = 5;      // Down / Up arrow
    public double SeekWheelSeconds { get; set; } = 3;       // wheel over the video
    public double SeekPageSeconds { get; set; } = 60;       // PgDown / PgUp
    public double SeekAltPageSeconds { get; set; } = 300;   // Alt + PgDown / PgUp

    // [Gallery] (F10, 1.6.0): image positions as percentages of each video's duration; ffmpeg runs at a time
    public static readonly double[] DefaultGalleryPercents = { 10, 30, 50, 70, 90 };
    public double[] GalleryPercents { get; set; } = (double[])DefaultGalleryPercents.Clone();
    public int GalleryJobs { get; set; } = 2;

    // [Cache]
    public int CacheMaxMB { get; set; } // 0 = mpv cache disabled (only the minimal demuxer read-ahead)

    // [Delete] (1.8.0): false = "Delete" in the video's right-click menu deletes one file without confirmation
    public bool ConfirmDeleteFromVideoMenu { get; set; }

    // [Diagnostics]
    public bool MetricsLog { get; set; } = true;

    public int LoadedSchemaVersion { get; private set; } = CurrentSchemaVersion;
    public List<string> Warnings { get; } = new();

    // ------------------------------------------------------------------ load

    public static AppSettings Load(string path)
    {
        var s = new AppSettings();
        string text;
        try
        {
            if (!File.Exists(path)) return s; // first run: defaults, no warning
            text = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            s.Warnings.Add($"{FileName} could not be read ({ex.Message}); using defaults.");
            return s;
        }

        s._ini = IniFile.Parse(text);
        s.ReadAll();
        return s;
    }

    private void ReadAll()
    {
        LoadedSchemaVersion = ReadInt("viBro", "SchemaVersion", CurrentSchemaVersion, 1, int.MaxValue);
        if (LoadedSchemaVersion > CurrentSchemaVersion)
            Warnings.Add($"{FileName} was written by a newer viBro (schema {LoadedSchemaVersion}); unknown settings are kept.");

        Theme = (ThemeChoice)ReadEnumWord("Appearance", "Theme", new[] { "System", "Light", "Dark" }, (int)Theme);

        WindowLeft = ReadNullableDouble("Window", "Left", -100000, 100000);
        WindowTop = ReadNullableDouble("Window", "Top", -100000, 100000);
        WindowWidth = ReadDouble("Window", "Width", WindowWidth, 400, 100000);
        WindowHeight = ReadDouble("Window", "Height", WindowHeight, 300, 100000);
        WindowMaximized = ReadEnumWord("Window", "State", new[] { "Normal", "Maximized" }, WindowMaximized ? 1 : 0) == 1;

        TreeWidth = ReadDouble("Layout", "TreeWidth", TreeWidth, 0, 10000);
        ListHeight = ReadDouble("Layout", "ListHeight", ListHeight, 0, 10000);

        SortColumn = (SortColumn)ReadEnumWord("List", "SortColumn", new[] { "Name", "Size", "Date" }, (int)SortColumn);
        SortDescending = ReadBool("List", "SortDescending", SortDescending);
        NameColumnWidth = ReadDouble("List", "NameWidth", NameColumnWidth, 20, 5000);
        SizeColumnWidth = ReadDouble("List", "SizeWidth", SizeColumnWidth, 20, 5000);
        DateColumnWidth = ReadDouble("List", "DateWidth", DateColumnWidth, 20, 5000);

        LastFolder = ReadString("Folders", "LastFolder");
        LastMoveDestination = ReadString("Folders", "LastMoveDestination");
        LastCopyDestination = ReadString("Folders", "LastCopyDestination");

        Volume = ReadDouble("Audio", "Volume", Volume, 0, 100);
        Mute = ReadBool("Audio", "Mute", Mute);

        HardwareDecoding = ReadHwdec("Playback", "HardwareDecoding", HardwareDecoding);
        ExactLongSeeks = ReadBool("Playback", "ExactLongSeeks", ExactLongSeeks);
        ExactSeekThresholdSeconds = ReadDouble("Playback", "ExactSeekThresholdSeconds", ExactSeekThresholdSeconds, 0, 3600);

        SeekLeftRightSeconds = ReadDouble("Seek", "LeftRightSeconds", SeekLeftRightSeconds, MinSeekSeconds, MaxSeekSeconds);
        SeekUpDownSeconds = ReadDouble("Seek", "UpDownSeconds", SeekUpDownSeconds, MinSeekSeconds, MaxSeekSeconds);
        SeekWheelSeconds = ReadDouble("Seek", "WheelSeconds", SeekWheelSeconds, MinSeekSeconds, MaxSeekSeconds);
        SeekPageSeconds = ReadDouble("Seek", "PageSeconds", SeekPageSeconds, MinSeekSeconds, MaxSeekSeconds);
        SeekAltPageSeconds = ReadDouble("Seek", "AltPageSeconds", SeekAltPageSeconds, MinSeekSeconds, MaxSeekSeconds);

        GalleryPercents = ReadPercentList("Gallery", "PositionsPercent", GalleryPercents);
        GalleryJobs = ReadInt("Gallery", "Jobs", GalleryJobs, 1, 8);

        CacheMaxMB = ReadInt("Cache", "MaxMB", CacheMaxMB, 0, 4096);

        ConfirmDeleteFromVideoMenu = ReadBool("Delete", "ConfirmFromVideoMenu", ConfirmDeleteFromVideoMenu);

        MetricsLog = ReadBool("Diagnostics", "MetricsLog", MetricsLog);
    }

    // ------------------------------------------------------------------ save

    /// <summary>Checks that viBro.ini can be created/replaced in its folder, without changing it.</summary>
    public static bool CanWrite(string path, out string? reason)
    {
        reason = null;
        try
        {
            if (File.Exists(path) && new FileInfo(path).IsReadOnly)
            {
                reason = $"{FileName} is read-only";
                return false;
            }
            string probe = path + ".probe";
            using (new FileStream(probe, FileMode.Create, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)) { }
            return true;
        }
        catch (Exception ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    /// <summary>Atomic save: write a temporary file, flush it to disk, then replace the INI.</summary>
    public void Save(string path)
    {
        WriteAll();
        string content = _ini.Serialize(
            "viBro settings. Edit only while viBro is closed; invalid values are replaced by defaults.");
        string tmp = path + ".tmp";
        using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var w = new StreamWriter(fs, new UTF8Encoding(false)))
        {
            w.Write(content);
            w.Flush();
            fs.Flush(flushToDisk: true);
        }
        if (File.Exists(path))
            File.Replace(tmp, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
        else
            File.Move(tmp, path);
    }

    private void WriteAll()
    {
        var c = CultureInfo.InvariantCulture;
        string D(double v) => Math.Round(v, 1).ToString("0.#", c);
        string B(bool v) => v ? "true" : "false";

        _ini.Set("viBro", "SchemaVersion", Math.Max(LoadedSchemaVersion, CurrentSchemaVersion).ToString(c));

        _ini.Set("Appearance", "Theme", Theme.ToString());

        if (WindowLeft is double l) _ini.Set("Window", "Left", D(l));
        if (WindowTop is double t) _ini.Set("Window", "Top", D(t));
        _ini.Set("Window", "Width", D(WindowWidth));
        _ini.Set("Window", "Height", D(WindowHeight));
        _ini.Set("Window", "State", WindowMaximized ? "Maximized" : "Normal");

        _ini.Set("Layout", "TreeWidth", D(TreeWidth));
        _ini.Set("Layout", "ListHeight", D(ListHeight));

        _ini.Set("List", "SortColumn", SortColumn.ToString());
        _ini.Set("List", "SortDescending", B(SortDescending));
        _ini.Set("List", "NameWidth", D(NameColumnWidth));
        _ini.Set("List", "SizeWidth", D(SizeColumnWidth));
        _ini.Set("List", "DateWidth", D(DateColumnWidth));

        _ini.Set("Folders", "LastFolder", LastFolder ?? "");
        _ini.Set("Folders", "LastMoveDestination", LastMoveDestination ?? "");
        _ini.Set("Folders", "LastCopyDestination", LastCopyDestination ?? "");

        _ini.Set("Audio", "Volume", D(Volume));
        _ini.Set("Audio", "Mute", B(Mute));

        _ini.Set("Playback", "HardwareDecoding", HardwareDecoding);
        _ini.Set("Playback", "ExactLongSeeks", B(ExactLongSeeks));
        _ini.Set("Playback", "ExactSeekThresholdSeconds", D(ExactSeekThresholdSeconds));

        _ini.Set("Seek", "LeftRightSeconds", D(SeekLeftRightSeconds));
        _ini.Set("Seek", "UpDownSeconds", D(SeekUpDownSeconds));
        _ini.Set("Seek", "WheelSeconds", D(SeekWheelSeconds));
        _ini.Set("Seek", "PageSeconds", D(SeekPageSeconds));
        _ini.Set("Seek", "AltPageSeconds", D(SeekAltPageSeconds));

        _ini.Set("Gallery", "PositionsPercent", string.Join(",", Array.ConvertAll(GalleryPercents, D)));
        _ini.Set("Gallery", "Jobs", GalleryJobs.ToString(c));

        _ini.Set("Cache", "MaxMB", CacheMaxMB.ToString(c));

        _ini.Set("Delete", "ConfirmFromVideoMenu", B(ConfirmDeleteFromVideoMenu));

        _ini.Set("Diagnostics", "MetricsLog", B(MetricsLog));
    }

    // ------------------------------------------------------------------ typed readers

    private void Invalid(string section, string key, string value)
        => Warnings.Add($"{FileName}: [{section}] {key}={value} is not valid; default used.");

    private string? ReadString(string section, string key)
    {
        string? v = _ini.Get(section, key);
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    private double ReadDouble(string section, string key, double def, double min, double max)
        => ReadNullableDouble(section, key, min, max) ?? def;

    private double? ReadNullableDouble(string section, string key, double min, double max)
    {
        string? v = _ini.Get(section, key);
        if (string.IsNullOrWhiteSpace(v)) return null;
        if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
            && !double.IsNaN(d) && d >= min && d <= max)
            return d;
        Invalid(section, key, v);
        return null;
    }

    /// <summary>"10,30,50,70,90": 1 to 10 values from 0 to 100, kept in the given order.</summary>
    private double[] ReadPercentList(string section, string key, double[] def)
    {
        string? v = _ini.Get(section, key);
        if (string.IsNullOrWhiteSpace(v)) return def;
        string[] parts = v.Split(',', StringSplitOptions.TrimEntries);
        var list = new List<double>();
        foreach (string p in parts)
        {
            if (!double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) || double.IsNaN(d) || d < 0 || d > 100)
            {
                list.Clear();
                break;
            }
            list.Add(d);
        }
        if (list.Count is >= 1 and <= 10) return list.ToArray();
        Invalid(section, key, v);
        return def;
    }

    private int ReadInt(string section, string key, int def, int min, int max)
    {
        string? v = _ini.Get(section, key);
        if (string.IsNullOrWhiteSpace(v)) return def;
        if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) && i >= min && i <= max)
            return i;
        Invalid(section, key, v);
        return def;
    }

    private bool ReadBool(string section, string key, bool def)
    {
        string? v = _ini.Get(section, key);
        if (string.IsNullOrWhiteSpace(v)) return def;
        switch (v.Trim().ToLowerInvariant())
        {
            case "true": case "1": case "yes": case "on": return true;
            case "false": case "0": case "no": case "off": return false;
        }
        Invalid(section, key, v);
        return def;
    }

    private int ReadEnumWord(string section, string key, string[] words, int def)
    {
        string? v = _ini.Get(section, key);
        if (string.IsNullOrWhiteSpace(v)) return def;
        for (int i = 0; i < words.Length; i++)
            if (string.Equals(words[i], v.Trim(), StringComparison.OrdinalIgnoreCase))
                return i;
        Invalid(section, key, v);
        return def;
    }

    private string ReadHwdec(string section, string key, string def)
    {
        string? v = _ini.Get(section, key);
        if (string.IsNullOrWhiteSpace(v)) return def;
        // mpv hwdec values: "auto-safe", "no", "d3d11va", or a comma-separated priority list.
        if (Regex.IsMatch(v, "^[a-z0-9-]+(,[a-z0-9-]+)*$")) return v;
        Invalid(section, key, v);
        return def;
    }
}
