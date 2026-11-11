using System;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ViBro.Util;

namespace ViBro;

// Look and feedback (1.2.0): icon buttons, time under the pointer on the timeline, the loaded file
// marked in the list, position in the folder, mpv OSD text in fullscreen.
public partial class MainWindow
{
    // Segoe Fluent Icons code points (the same in Segoe MDL2 Assets, used as fallback).
    private const string PlayGlyph = "", PauseGlyph = "";
    private const string VolumeGlyph = "", MuteGlyph = "";
    private const string SunGlyph = "", MoonGlyph = "";

    private const int TimeOsdMs = 1000;    // time shown over the video after a seek (fullscreen)
    private const int FileNameOsdMs = 800; // file name shown over the video after a change (fullscreen)

    private VideoItem? _currentItem; // the file loaded in the player
    private bool? _shownPaused, _shownMuted;

    /// <summary>Marks the loaded file in the list (play/pause glyph and semibold name).</summary>
    private void SetCurrentItem(VideoItem? item)
    {
        if (!ReferenceEquals(_currentItem, item)) _currentItem?.SetCurrent(false, true);
        bool changed = !ReferenceEquals(_currentItem, item);
        _currentItem = item;
        item?.SetCurrent(true, _player?.IsPaused ?? true);
        if (!changed) return;
        if (item == null) MediaText.Text = "";
        foreach (var g in OpenGalleries) g.SetCurrent(item?.FullPath); // 1.13.0: the loaded row is marked
        UpdateVideoPlaceholder();
    }

    /// <summary>Glyphs and tooltips follow the player state; updated only when it changes.</summary>
    private void UpdateTransportIcons(bool paused, bool muted)
    {
        if (_shownPaused != paused)
        {
            _shownPaused = paused;
            PlayButton.Content = paused ? PlayGlyph : PauseGlyph;
            PlayButton.ToolTip = (paused ? "Play" : "Pause") + " (Space, or click on the video)";
        }
        if (_shownMuted != muted)
        {
            _shownMuted = muted;
            MuteButton.Content = muted ? MuteGlyph : VolumeGlyph;
            MuteButton.ToolTip = (muted ? "Unmute" : "Mute") + " (M)";
        }
    }

    /// <summary>Position and time over the video for ~1 s after a seek, only in fullscreen.</summary>
    private void ShowTimeOsd(double position)
    {
        if (!_isFullscreen || _player == null) return;
        double duration = _player.Duration;
        _player.ShowText(FormatTime(Math.Clamp(position, 0, Math.Max(duration, 0))) + " / " + FormatTime(duration), TimeOsdMs);
    }

    // ---------------------------------------------------------------- time under the pointer

    private void TimeSlider_MouseMove(object sender, MouseEventArgs e)
    {
        double duration = _player?.Duration ?? 0;
        if (duration <= 0 || TimeSlider.Template?.FindName("PART_Track", TimeSlider) is not Track track)
        {
            TimeHoverPopup.IsOpen = false;
            return;
        }
        double value = Math.Clamp(track.ValueFromPoint(e.GetPosition(track)), 0, duration);
        TimeHoverText.Text = FormatTime(value);
        if (!TimeHoverPopup.IsOpen)
        {
            TimeHoverBorder.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, ThemeManager.PanelKey);
            TimeHoverBorder.SetResourceReference(System.Windows.Controls.Border.BorderBrushProperty, ThemeManager.ForegroundKey);
            TimeHoverText.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, ThemeManager.ForegroundKey);
            TimeHoverPopup.IsOpen = true;
        }
        // Centred on the pointer, just above the bar.
        TimeHoverPopup.HorizontalOffset = e.GetPosition(TimeSlider).X - TimeHoverBorder.ActualWidth / 2;
        TimeHoverPopup.VerticalOffset = -4;
    }

    private void TimeSlider_MouseLeave(object sender, MouseEventArgs e) => TimeHoverPopup.IsOpen = false;
}
