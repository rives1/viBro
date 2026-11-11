using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ViBro.Util;

namespace ViBro;

/// <summary>Weight of a status message (1.13.0).</summary>
internal enum StatusKind
{
    Normal,  // progress and information: replaced by the next message
    Success, // result of an operation: green, stays 6 s
    Error,   // error or warning: orange, stays 6 s
}

// Status bar messages (1.13.0). Results and errors stay at least 6 s: a normal message arriving
// meanwhile (e.g. "Loading ...") waits and is shown when the time is over, so an error is not lost at
// the next load. A click on the message shows the last 20 messages (progress messages ending in "..."
// are not kept).
public partial class MainWindow
{
    private const int StatusHoldMs = 6000;
    private const int StatusHistoryMax = 20;

    private static readonly Brush ErrorBrush = FrozenBrush(0xE8, 0x87, 0x1E);
    private static readonly Brush SuccessBrush = FrozenBrush(0x3D, 0xA3, 0x5D);

    private readonly List<(DateTime Time, string Text, StatusKind Kind)> _statusHistory = new();
    private DispatcherTimer? _statusHoldTimer;
    private bool _statusHeld;
    private string? _pendingStatus;

    private void SetStatus(string text, StatusKind kind = StatusKind.Normal)
    {
        if (kind == StatusKind.Normal && _statusHeld)
        {
            _pendingStatus = text; // shown when the important message has had its time
            return;
        }
        ShowStatus(text, kind);
        if (kind == StatusKind.Normal) return;

        _statusHeld = true;
        _pendingStatus = null;
        if (_statusHoldTimer == null)
        {
            _statusHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(StatusHoldMs) };
            _statusHoldTimer.Tick += (_, _) =>
            {
                _statusHoldTimer!.Stop();
                _statusHeld = false;
                if (_pendingStatus != null) ShowStatus(_pendingStatus, StatusKind.Normal);
                _pendingStatus = null;
            };
        }
        _statusHoldTimer.Stop();
        _statusHoldTimer.Start();
    }

    private void ShowStatus(string text, StatusKind kind)
    {
        StatusText.Text = text;
        if (kind == StatusKind.Normal)
        {
            StatusText.ClearValue(TextBlock.ForegroundProperty); // the status bar's colour (theme)
            StatusText.FontWeight = FontWeights.Normal;
        }
        else
        {
            StatusText.Foreground = kind == StatusKind.Error ? ErrorBrush : SuccessBrush;
            StatusText.FontWeight = FontWeights.SemiBold;
        }
        if (string.IsNullOrWhiteSpace(text) || text.TrimEnd().EndsWith("...", StringComparison.Ordinal)) return;
        if (_statusHistory.Count > 0 && _statusHistory[^1].Text == text) return; // the same message again
        _statusHistory.Add((DateTime.Now, text, kind));
        if (_statusHistory.Count > StatusHistoryMax) _statusHistory.RemoveAt(0);
    }

    /// <summary>Click on the message: the last 20 messages, newest first, with their time.</summary>
    private void StatusText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        StatusHistoryBorder.SetResourceReference(Border.BackgroundProperty, ThemeManager.PanelKey);
        StatusHistoryBorder.SetResourceReference(TextElement.ForegroundProperty, ThemeManager.ForegroundKey);
        StatusHistoryList.Children.Clear();
        if (_statusHistory.Count == 0)
            StatusHistoryList.Children.Add(new TextBlock { Text = "No messages yet.", Opacity = 0.7 });
        for (int i = _statusHistory.Count - 1; i >= 0; i--)
        {
            var (time, text, kind) = _statusHistory[i];
            var line = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1) };
            line.Inlines.Add(new System.Windows.Documents.Run(time.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "   ")
            {
                FontFamily = new FontFamily("Consolas"),
            });
            var run = new System.Windows.Documents.Run(text);
            if (kind == StatusKind.Error) run.Foreground = ErrorBrush;
            else if (kind == StatusKind.Success) run.Foreground = SuccessBrush;
            line.Inlines.Add(run);
            StatusHistoryList.Children.Add(line);
        }
        StatusHistoryPopup.IsOpen = true;
        e.Handled = true;
    }

    private static Brush FrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
