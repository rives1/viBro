using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

    public const string ForegroundKey = "ViBro.Foreground";
    public const string BackgroundKey = "ViBro.Background";
    public const string PanelKey = "ViBro.Panel";

    /// <summary>
    /// Fluent styles each control, but its text colour for plain text comes from an implicit Window
    /// style that WPF does not apply to derived windows (MainWindow, dialogs): text stayed black on
    /// the dark background. viBro's windows therefore take text and background colours from these
    /// application resources, updated on every theme change.
    /// </summary>
    public static void AttachWindow(Window w)
    {
        w.SetResourceReference(Control.ForegroundProperty, ForegroundKey);
        w.SetResourceReference(Control.BackgroundProperty, BackgroundKey);
    }

    public static void Apply(ThemeChoice choice)
    {
        bool dark = IsDark(choice);
        var res = Application.Current.Resources;
        res[ForegroundKey] = Frozen(dark ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x1A, 0x1A, 0x1A));
        res[BackgroundKey] = Frozen(dark ? Color.FromRgb(0x20, 0x20, 0x20) : Color.FromRgb(0xF3, 0xF3, 0xF3));
        res[PanelKey] = Frozen(dark ? Color.FromRgb(0x2B, 0x2B, 0x2B) : Color.FromRgb(0xE6, 0xE6, 0xE6));

#pragma warning disable WPF0001 // ThemeMode is marked experimental in WPF
        Application.Current.ThemeMode = choice switch
        {
            ThemeChoice.Light => ThemeMode.Light,
            ThemeChoice.Dark => ThemeMode.Dark,
            _ => ThemeMode.System,
        };
#pragma warning restore WPF0001
    }

    private static SolidColorBrush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
