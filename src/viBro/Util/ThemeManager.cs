using System;
using System.Windows;
using Microsoft.Win32;
using ViBro.Settings;

namespace ViBro.Util;

/// <summary>
/// Light/Dark themes through WPF's built-in Fluent theme (Application.ThemeMode), which restyles
/// every standard control, the dialogs and the window title bar consistently.
/// </summary>
internal static class ThemeManager
{
    /// <summary>Windows "app mode" (Settings > Personalization > Colors). Defaults to light if unknown.</summary>
    public static bool WindowsUsesLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is not int v || v != 0;
        }
        catch
        {
            return true;
        }
    }

    public static bool IsDark(ThemeChoice choice)
        => choice == ThemeChoice.Dark || (choice == ThemeChoice.System && !WindowsUsesLightTheme());

    public static void Apply(ThemeChoice choice)
    {
#pragma warning disable WPF0001 // ThemeMode is marked experimental in WPF
        Application.Current.ThemeMode = choice switch
        {
            ThemeChoice.Light => ThemeMode.Light,
            ThemeChoice.Dark => ThemeMode.Dark,
            _ => ThemeMode.System,
        };
#pragma warning restore WPF0001
    }
}
