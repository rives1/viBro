using System;
using System.Collections.Generic;

namespace ViBro.Util;

/// <summary>
/// Command line (prototype only):
///   viBro.exe [folder] [--exact-seeks] [--mpv-opt name=value]...
/// --mpv-opt values override viBro's defaults and are meant for performance experiments.
/// </summary>
internal sealed class StartupOptions
{
    public string? Folder { get; private set; }
    public bool ExactSeeks { get; private set; }
    public List<KeyValuePair<string, string>> MpvOptions { get; } = new();
    public List<string> Errors { get; } = new();

    public static StartupOptions Parse(string[] args)
    {
        var o = new StartupOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a.Equals("--exact-seeks", StringComparison.OrdinalIgnoreCase))
            {
                o.ExactSeeks = true;
            }
            else if (a.StartsWith("--mpv-opt=", StringComparison.OrdinalIgnoreCase))
            {
                o.AddMpvOption(a.Substring("--mpv-opt=".Length));
            }
            else if (a.Equals("--mpv-opt", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length) o.AddMpvOption(args[++i]);
                else o.Errors.Add("--mpv-opt requires name=value");
            }
            else if (a.StartsWith("--", StringComparison.Ordinal))
            {
                o.Errors.Add("Unknown option: " + a);
            }
            else if (o.Folder == null)
            {
                o.Folder = a.Trim().Trim('"');
            }
        }
        return o;
    }

    private void AddMpvOption(string nameValue)
    {
        int eq = nameValue.IndexOf('=');
        if (eq <= 0)
        {
            Errors.Add("Invalid --mpv-opt (expected name=value): " + nameValue);
            return;
        }
        MpvOptions.Add(new KeyValuePair<string, string>(nameValue.Substring(0, eq), nameValue.Substring(eq + 1)));
    }
}
