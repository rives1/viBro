using System;
using System.IO;
using System.Reflection;

namespace ViBro.Util;

internal static class AppInfo
{
    /// <summary>Folder of viBro.exe (not the single-file extraction folder).</summary>
    public static string ExeDirectory { get; } =
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;

    /// <summary>Version from Directory.Build.props, plus "+commit" when built from git.</summary>
    public static string InformationalVersion { get; } =
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";

    /// <summary>"0.1.0 (abc1234)" or "0.1.0".</summary>
    public static string DisplayVersion
    {
        get
        {
            string v = InformationalVersion;
            int plus = v.IndexOf('+');
            if (plus < 0) return v;
            string commit = v.Substring(plus + 1);
            return commit.Length >= 7 ? $"{v.Substring(0, plus)} ({commit.Substring(0, 7)})" : v.Substring(0, plus);
        }
    }

    public static string SemanticVersion
    {
        get
        {
            string v = InformationalVersion;
            int plus = v.IndexOf('+');
            return plus < 0 ? v : v.Substring(0, plus);
        }
    }
}
