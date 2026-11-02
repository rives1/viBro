using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ViBro.Settings;

/// <summary>
/// Minimal INI document: [Section] / key=value / ';' or '#' comments.
/// Keeps section and key order, and keeps keys it does not know about,
/// so a file edited by hand or by a newer version is not stripped on save.
/// </summary>
internal sealed class IniFile
{
    private readonly List<Section> _sections = new();

    private sealed class Section
    {
        public Section(string name) => Name = name;
        public string Name { get; }
        public List<KeyValuePair<string, string>> Entries { get; } = new();
    }

    public static IniFile Parse(string text)
    {
        var ini = new IniFile();
        Section? current = null;
        using var reader = new StringReader(text);
        string? raw;
        while ((raw = reader.ReadLine()) != null)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;

            if (line[0] == '[' && line[^1] == ']')
            {
                current = ini.GetOrAddSection(line.Substring(1, line.Length - 2).Trim());
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq <= 0) continue; // not key=value: ignored
            current ??= ini.GetOrAddSection("");
            string key = line.Substring(0, eq).Trim();
            string value = line.Substring(eq + 1).Trim();
            SetIn(current, key, value);
        }
        return ini;
    }

    public string? Get(string section, string key)
    {
        var s = FindSection(section);
        if (s == null) return null;
        foreach (var e in s.Entries)
            if (string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase))
                return e.Value;
        return null;
    }

    public void Set(string section, string key, string value)
        => SetIn(GetOrAddSection(section), key, value);

    public string Serialize(string? headerComment = null)
    {
        var sb = new StringBuilder();
        if (headerComment != null)
        {
            foreach (var l in headerComment.Split('\n'))
                sb.Append("; ").Append(l.TrimEnd('\r')).Append("\r\n");
            sb.Append("\r\n");
        }
        foreach (var s in _sections)
        {
            if (s.Entries.Count == 0) continue;
            if (s.Name.Length > 0) sb.Append('[').Append(s.Name).Append("]\r\n");
            foreach (var e in s.Entries)
                sb.Append(e.Key).Append('=').Append(e.Value).Append("\r\n");
            sb.Append("\r\n");
        }
        return sb.ToString();
    }

    private Section? FindSection(string name)
    {
        foreach (var s in _sections)
            if (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                return s;
        return null;
    }

    private Section GetOrAddSection(string name)
    {
        var s = FindSection(name);
        if (s != null) return s;
        s = new Section(name);
        _sections.Add(s);
        return s;
    }

    private static void SetIn(Section s, string key, string value)
    {
        // Line breaks would corrupt the file; paths and numbers never contain them.
        value = value.Replace("\r", "").Replace("\n", "");
        for (int i = 0; i < s.Entries.Count; i++)
        {
            if (string.Equals(s.Entries[i].Key, key, StringComparison.OrdinalIgnoreCase))
            {
                s.Entries[i] = new KeyValuePair<string, string>(s.Entries[i].Key, value);
                return;
            }
        }
        s.Entries.Add(new KeyValuePair<string, string>(key, value));
    }
}
