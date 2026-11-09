using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace ViBro.Settings;

/// <summary>Keyboard commands of the main window (1.10.0: keys configurable in [Keys] of viBro.ini).</summary>
internal enum Command
{
    SeekBackShort, SeekForwardShort, SeekBackMedium, SeekForwardMedium,
    SeekBackLong, SeekForwardLong, SeekBackVeryLong, SeekForwardVeryLong,
    PlayPause, PreviousVideo, NextVideo, Mute, Fullscreen, ExitFullscreen,
    FocusPath, SelectAll, Refresh, OpenFolder, OpenWithDefaultApp,
    Delete, Rename, Move, MoveToLast, Copy, CopyToLast,
    MarkIn, MarkOut, Extract, Gallery, Commands, Close,
}

/// <summary>A key with its exact modifiers (Ctrl, Alt, Shift; the Windows key is not used).</summary>
internal readonly record struct KeyChord(Key Key, ModifierKeys Modifiers)
{
    public const ModifierKeys Used = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift;
}

/// <summary>
/// [Keys] in viBro.ini: one line per command, "Command=Key" or several keys separated by commas,
/// e.g. "Delete=Delete,R" or "MoveToLast=Shift+W"; "none" leaves a command without keys.
/// Modifiers must match exactly. A value that cannot be read gives a warning and the default; a key
/// given to two commands stays with the one listed first (the order of the file) and the other loses
/// it, with a warning. Fixed keys that are not shortcuts (Enter / Esc in the path bar and in dialogs,
/// Esc in the gallery) are not part of this table.
/// </summary>
internal sealed class KeyBindings
{
    public const string Section = "Keys";

    private sealed record Spec(Command Command, string Defaults, bool Repeats);

    // Order = order in the file and priority in conflicts.
    private static readonly Spec[] Table =
    {
        new(Command.SeekBackShort, "Left", true),
        new(Command.SeekForwardShort, "Right", true),
        new(Command.SeekBackMedium, "Down", true),
        new(Command.SeekForwardMedium, "Up", true),
        new(Command.SeekBackLong, "PageDown", true),
        new(Command.SeekForwardLong, "PageUp", true),
        new(Command.SeekBackVeryLong, "Alt+PageDown", true),
        new(Command.SeekForwardVeryLong, "Alt+PageUp", true),
        new(Command.PlayPause, "Space", false),
        new(Command.PreviousVideo, "E", true),
        new(Command.NextVideo, "D", true),
        new(Command.Mute, "M", false),
        new(Command.Fullscreen, "F11", false),
        new(Command.ExitFullscreen, "Escape", true),
        new(Command.FocusPath, "Ctrl+L", true),
        new(Command.SelectAll, "Ctrl+A", true),
        new(Command.Refresh, "F5", false),
        new(Command.OpenFolder, "Enter", false),
        new(Command.OpenWithDefaultApp, "Shift+Enter", false),
        new(Command.Delete, "Delete,R", false),
        new(Command.Rename, "F2", false),
        new(Command.Move, "W", false),
        new(Command.MoveToLast, "Shift+W", false),
        new(Command.Copy, "C", false),
        new(Command.CopyToLast, "Shift+C", false),
        new(Command.MarkIn, "I", false),
        new(Command.MarkOut, "O", false),
        new(Command.Extract, "F9", false),
        new(Command.Gallery, "F10", false),
        new(Command.Commands, "Ctrl+F2", false),
        new(Command.Close, "Ctrl+W", false),
    };

    private static readonly Dictionary<string, Key> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Del"] = Key.Delete, ["Esc"] = Key.Escape, ["Return"] = Key.Enter, ["Ins"] = Key.Insert,
        ["PgUp"] = Key.PageUp, ["PgDn"] = Key.PageDown, ["PgDown"] = Key.PageDown,
        ["Prior"] = Key.PageUp, ["Next"] = Key.PageDown,
        ["0"] = Key.D0, ["1"] = Key.D1, ["2"] = Key.D2, ["3"] = Key.D3, ["4"] = Key.D4,
        ["5"] = Key.D5, ["6"] = Key.D6, ["7"] = Key.D7, ["8"] = Key.D8, ["9"] = Key.D9,
    };

    private static readonly HashSet<Key> NotAllowed = new()
    {
        Key.None, Key.System, Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt,
        Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin, Key.ImeProcessed, Key.DeadCharProcessed,
    };

    private readonly Dictionary<KeyChord, Command> _byChord = new();
    private readonly Dictionary<Command, List<KeyChord>> _byCommand = new();

    public static KeyBindings Defaults()
    {
        var b = new KeyBindings();
        foreach (var s in Table) b.Assign(s.Command, ParseList(s.Defaults)!, null);
        return b;
    }

    /// <summary>Reads [Keys]; missing lines take the default.</summary>
    public static KeyBindings Read(Func<string, string?> get, List<string> warnings, string fileName)
    {
        var b = new KeyBindings();
        foreach (var s in Table)
        {
            string name = s.Command.ToString();
            string? value = get(name);
            List<KeyChord>? chords = string.IsNullOrWhiteSpace(value) ? null : ParseList(value);
            if (chords == null)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    warnings.Add($"{fileName}: [{Section}] {name}={value} is not valid; default used.");
                chords = ParseList(s.Defaults)!;
            }
            b.Assign(s.Command, chords, conflict =>
                warnings.Add($"{fileName}: [{Section}] {name}: {Format(conflict)} is already used by {b._byChord[conflict]}; ignored for {name}."));
        }
        return b;
    }

    /// <summary>Lines for the file, in table order.</summary>
    public IEnumerable<KeyValuePair<string, string>> Lines()
    {
        foreach (var s in Table)
        {
            var list = _byCommand.TryGetValue(s.Command, out var l) ? l : new List<KeyChord>();
            yield return new(s.Command.ToString(), list.Count == 0 ? "none" : string.Join(",", list.Select(Format)));
        }
    }

    public bool TryGet(Key key, ModifierKeys modifiers, out Command command)
        => _byChord.TryGetValue(new KeyChord(key, modifiers & KeyChord.Used), out command);

    public static bool AllowsRepeat(Command c) => Table.First(s => s.Command == c).Repeats;

    /// <summary>For Ctrl+F2: "Del or R", "Ctrl + F2", "←"; "(no key)" when none.</summary>
    public string Display(Command c)
    {
        var list = _byCommand.TryGetValue(c, out var l) ? l : new List<KeyChord>();
        return list.Count == 0 ? "(no key)" : string.Join(" or ", list.Select(DisplayChord));
    }

    /// <summary>"← / →" for a pair of commands (back / forward, previous / next).</summary>
    public string DisplayPair(Command a, Command b) => Display(a) + " / " + Display(b);

    private void Assign(Command c, List<KeyChord> chords, Action<KeyChord>? onConflict)
    {
        var mine = new List<KeyChord>();
        foreach (var chord in chords)
        {
            if (_byChord.ContainsKey(chord))
            {
                if (_byChord[chord] != c) onConflict?.Invoke(chord);
                continue;
            }
            _byChord[chord] = c;
            mine.Add(chord);
        }
        _byCommand[c] = mine;
    }

    // ------------------------------------------------------------------ parsing and formatting

    /// <summary>"Delete,R", "Ctrl+Shift+W", "none" (empty list). Null if any part cannot be read.</summary>
    public static List<KeyChord>? ParseList(string value)
    {
        if (string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase)) return new List<KeyChord>();
        var list = new List<KeyChord>();
        foreach (string part in value.Split(',', StringSplitOptions.TrimEntries))
        {
            if (TryParseChord(part, out var chord)) { if (!list.Contains(chord)) list.Add(chord); }
            else return null;
        }
        return list.Count > 0 ? list : null;
    }

    public static bool TryParseChord(string text, out KeyChord chord)
    {
        chord = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        string[] parts = text.Split('+', StringSplitOptions.TrimEntries);
        var mods = ModifierKeys.None;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            ModifierKeys m = parts[i].ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierKeys.Control,
                "alt" => ModifierKeys.Alt,
                "shift" => ModifierKeys.Shift,
                _ => ModifierKeys.None,
            };
            if (m == ModifierKeys.None || (mods & m) != 0) return false;
            mods |= m;
        }
        string keyName = parts[^1];
        if (keyName.Length == 0) return false;
        if (!Aliases.TryGetValue(keyName, out Key key))
        {
            // Names of System.Windows.Input.Key, without numbers ("65" would parse as an enum value).
            if (char.IsDigit(keyName[0]) || !Enum.TryParse(keyName, ignoreCase: true, out key) || !Enum.IsDefined(key))
                return false;
        }
        if (NotAllowed.Contains(key)) return false;
        chord = new KeyChord(key, mods);
        return true;
    }

    private static string KeyName(Key k) => k switch
    {
        Key.Enter => "Enter",       // same value as Return
        Key.PageUp => "PageUp",     // same value as Prior
        Key.PageDown => "PageDown", // same value as Next
        Key.CapsLock => "CapsLock",
        >= Key.D0 and <= Key.D9 => ((int)(k - Key.D0)).ToString(),
        _ => k.ToString(),
    };

    private static string ModifierPrefix(ModifierKeys m, string sep) =>
        ((m & ModifierKeys.Control) != 0 ? "Ctrl" + sep : "") +
        ((m & ModifierKeys.Alt) != 0 ? "Alt" + sep : "") +
        ((m & ModifierKeys.Shift) != 0 ? "Shift" + sep : "");

    public static string Format(KeyChord c) => ModifierPrefix(c.Modifiers, "+") + KeyName(c.Key);

    private static string DisplayChord(KeyChord c)
    {
        string key = c.Key switch
        {
            Key.Left => "←", Key.Right => "→", Key.Up => "↑", Key.Down => "↓",
            Key.PageUp => "PgUp", Key.PageDown => "PgDown", Key.Delete => "Del", Key.Escape => "Esc",
            _ => KeyName(c.Key),
        };
        return ModifierPrefix(c.Modifiers, " + ") + key;
    }
}
