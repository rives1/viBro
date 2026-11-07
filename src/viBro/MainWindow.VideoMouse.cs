using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using ViBro.Util;

namespace ViBro;

// Mouse on the video. The video is mpv's native window (mouse input disabled in mpv), so WPF gets
// no mouse events from it: the messages are read in OnThreadPreprocessMessage and handled here.
//  - single click: Play/Pause at once; double click: fullscreen on/off and the pause toggle of
//    its first click is undone (the video only stops for an instant)
//  - the click that brings viBro back to the foreground only activates the window
//  - right click: menu with Delete, Move..., Move to <last folder>, Copy..., Copy to <last folder>
//  - right button held + horizontal drag: moves in the video; the whole video width = the whole
//    duration, keyframe seeks while dragging and an exact seek on release (no menu then)
//  - wheel over the time bar (both modes): up = next video, down = previous (folders skipped; inverted in 1.9.0)
//  - right button held + wheel on the video (1.9.0): change video like the wheel on the time bar; no menu on release
//  - the wheel works also when viBro is not the foreground window (1.9.0)
//  - wheel over the video (both modes; in fullscreen anywhere but the bar): up = forward 3 s, down = back 3 s
public partial class MainWindow
{
    private uint _lastClickTime;
    private int _lastClickX, _lastClickY;
    private bool _hasLastClick;
    private bool _lastClickToggledPause;
    private uint _activatedTick; // GetTickCount time of the last window activation
    private int _wheelAccumulator;
    private bool _wheelSeeks; // what the accumulated wheel steps are for: seek (true) or change video

    // Right-button drag (scrubbing).
    private bool _rightDown;      // right button pressed on the video, mouse captured by the window
    private bool _scrubbing;      // the drag went past the threshold: no menu on release
    private bool _rightWheelUsed; // the wheel changed video while the right button was held: no menu, no scrub
    private int _rightDownX, _rightDownY;
    private double _scrubStartTime, _scrubDuration, _scrubTarget;
    private const double ScrubThresholdDip = 6; // smaller movements are still a right click

    private void OnVideoMouseMessage(MSG msg)
    {
        switch (msg.message)
        {
            case NativeMethods.WM_MOUSEMOVE:
                // Windows also sends WM_MOUSEMOVE without real movement: compare screen positions.
                if (_isFullscreen && (msg.pt_x != _lastMouseX || msg.pt_y != _lastMouseY))
                {
                    _lastMouseX = msg.pt_x;
                    _lastMouseY = msg.pt_y;
                    ShowFullscreenBar();
                }
                if (_rightDown)
                {
                    if ((msg.wParam.ToInt64() & NativeMethods.MK_RBUTTON) == 0) EndRightDrag(showMenu: false); // released unseen
                    else OnRightDrag(msg.pt_x, msg.pt_y);
                }
                break;

            case NativeMethods.WM_RBUTTONDOWN:
                BeginRightDrag(msg.pt_x, msg.pt_y);
                break;

            // mpv's window may or may not have CS_DBLCLKS: the second click arrives as WM_LBUTTONDOWN
            // or WM_LBUTTONDBLCLK. Both are treated as a click and double clicks are detected here,
            // with the system double-click time and distance.
            case NativeMethods.WM_LBUTTONDOWN:
            case NativeMethods.WM_LBUTTONDBLCLK:
                OnVideoClick(unchecked((uint)msg.time), msg.pt_x, msg.pt_y);
                break;

            case NativeMethods.WM_RBUTTONUP:
                if (_rightDown) EndRightDrag(showMenu: true);
                else ShowVideoMenu();
                break;
        }
    }

    private void BeginRightDrag(int x, int y)
    {
        _rightDown = true;
        _scrubbing = false;
        _rightWheelUsed = false;
        _rightDownX = x;
        _rightDownY = y;
        _scrubStartTime = _player?.TimePos ?? 0;
        _scrubDuration = _player?.Duration ?? 0;
        _scrubTarget = _scrubStartTime;
        // Windows does not capture the mouse for the right button: without this, releasing it
        // outside the video would be lost. The main window belongs to this thread.
        NativeMethods.SetCapture(new WindowInteropHelper(this).Handle);
    }

    private void OnRightDrag(int x, int y)
    {
        if (_rightWheelUsed || _player == null || _scrubDuration <= 0 || VideoHost.ActualWidth <= 0) return;
        // Distances in DIPs, relative to the video, so the scale follows the video's width.
        double dx = VideoHost.PointFromScreen(new Point(x, y)).X - VideoHost.PointFromScreen(new Point(_rightDownX, _rightDownY)).X;
        if (!_scrubbing)
        {
            if (Math.Abs(dx) < ScrubThresholdDip) return;
            _scrubbing = true;
        }
        double target = Math.Clamp(_scrubStartTime + dx / VideoHost.ActualWidth * _scrubDuration, 0, _scrubDuration);
        if (Math.Abs(target - _scrubTarget) < 0.05) return;
        _scrubTarget = target;
        _player.SeekAbsolute(target, exact: false); // keyframes while dragging; requests are coalesced
        ShowScrubPosition(target);
    }

    private void EndRightDrag(bool showMenu)
    {
        bool wasScrubbing = _scrubbing, wheelUsed = _rightWheelUsed;
        _rightDown = false;
        _scrubbing = false;
        _rightWheelUsed = false;
        NativeMethods.ReleaseCapture();
        if (wheelUsed) return; // right button + wheel changed video: neither menu nor seek
        if (wasScrubbing)
            _player?.SeekAbsolute(_scrubTarget, exact: true); // land exactly where the drag ended
        else if (showMenu)
            ShowVideoMenu();
    }

    /// <summary>While dragging, the bar and the time show the target, not the (lagging) decoded position.</summary>
    private void ShowScrubPosition(double target)
    {
        _updatingTimeSlider = true;
        TimeSlider.Maximum = _scrubDuration;
        TimeSlider.Value = target;
        _updatingTimeSlider = false;
        TimeText.Text = FormatTime(target) + " / " + FormatTime(_scrubDuration);
        ShowFullscreenBar();
        ShowTimeOsd(target);
    }

    /// <summary>Called on Window.Activated (WM_ACTIVATE is handled before the click that caused it).</summary>
    private void RecordActivation() => _activatedTick = unchecked((uint)Environment.TickCount);

    private void OnVideoClick(uint time, int x, int y)
    {
        var (w, h) = NativeMethods.DoubleClickSize();
        bool isDouble = _hasLastClick
                        && unchecked(time - _lastClickTime) <= NativeMethods.GetDoubleClickTime()
                        && Math.Abs(x - _lastClickX) <= w / 2
                        && Math.Abs(y - _lastClickY) <= h / 2;
        if (isDouble)
        {
            _hasLastClick = false; // a third click starts a new pair
            if (_lastClickToggledPause) _player?.TogglePause(); // undo the first click's Play/Pause
            ToggleFullscreen();
            return;
        }
        _hasLastClick = true;
        _lastClickTime = time;
        _lastClickX = x;
        _lastClickY = y;

        // The click's input time precedes the activation it caused: a click at most 50 ms after the
        // activation is the activating click and only brings the window to the foreground.
        bool activatingClick = (int)unchecked(time - _activatedTick) <= 50;
        _lastClickToggledPause = !activatingClick && _player != null;
        if (_lastClickToggledPause) _player!.TogglePause();
    }

    /// <summary>Screen point (device pixels) inside the visible video area.</summary>
    private bool IsPointOverVideo(int screenX, int screenY) => IsPointOver(VideoHost, screenX, screenY);

    private static bool IsPointOver(FrameworkElement element, int screenX, int screenY)
    {
        if (!element.IsVisible || PresentationSource.FromVisual(element) == null) return false;
        Point p = element.PointFromScreen(new Point(screenX, screenY));
        return p.X >= 0 && p.Y >= 0 && p.X < element.ActualWidth && p.Y < element.ActualHeight;
    }

    private void TraceInputMessage(MSG msg)
    {
        string what = msg.message == NativeMethods.WM_MOUSEWHEEL
            ? $"WM_MOUSEWHEEL delta={NativeMethods.WheelDelta(msg.wParam)}"
            : $"WM_KEYDOWN vk=0x{msg.wParam.ToInt64():X2} ({KeyInterop.KeyFromVirtualKey((int)msg.wParam.ToInt64())})";
        ErrorLog.Write("input", $"{what} hwnd=0x{msg.hwnd.ToInt64():X} class={NativeMethods.ClassName(msg.hwnd)} " +
                                $"pt=({msg.pt_x},{msg.pt_y}) overTimeBar={IsPointOver(TimeSlider, msg.pt_x, msg.pt_y)} " +
                                $"fullscreen={_isFullscreen}");
    }

    /// <summary>Main window or one of its children (the video, mpv's window).</summary>
    private bool IsOwnWindowMessage(IntPtr hwnd)
    {
        IntPtr main = new WindowInteropHelper(this).Handle;
        return main != IntPtr.Zero && (hwnd == main || NativeMethods.IsChild(main, hwnd));
    }

    /// <summary>
    /// One wheel notch (120) = one step; smaller steps (touchpads) are accumulated (1.3.0, user's choice):
    /// over the time bar, change video (down = next); over the video, and in fullscreen anywhere but the
    /// bar, seek by [Seek] WheelSeconds (default 3 s, up = forward). Elsewhere in the normal window (file list, tree) the wheel scrolls.
    /// </summary>
    private bool OnWheel(int delta, int screenX, int screenY)
    {
        // 1.9.0: not only when viBro is the foreground window. Windows sends the wheel to the window
        // under the pointer when "Scroll inactive windows when I hover over them" is on (the default).
        // Never while one of viBro's modal windows is open or a transfer has disabled the window.
        if (delta == 0 || !IsEnabled || ComponentDispatcher.IsThreadModal
            || !NativeMethods.IsWindowEnabled(new WindowInteropHelper(this).Handle)) return false;

        // Right button held on the video + wheel (1.9.0): change video, as on the time bar.
        if (_rightDown)
        {
            if (!_rightWheelUsed)
            {
                _rightWheelUsed = true;
                _scrubbing = false; // a drag on the previous file stops here; nothing is sought on release
                _wheelAccumulator = 0;
            }
            AccumulateWheel(delta, seek: false);
            return true;
        }

        bool onTimeBar = IsPointOver(TimeSlider, screenX, screenY);
        if (!onTimeBar && !_isFullscreen && !IsPointOverVideo(screenX, screenY)) return false;
        AccumulateWheel(delta, seek: !onTimeBar);
        return true;
    }

    /// <summary>
    /// Whole notches only (touchpads send smaller steps). Seek: up = forward. Change video (time bar,
    /// or right button held): up = next, down = previous (1.9.0; before, down was next).
    /// </summary>
    private void AccumulateWheel(int delta, bool seek)
    {
        // Restart counting when the direction or the target (seek / change video) changes.
        if (Math.Sign(delta) != Math.Sign(_wheelAccumulator) || seek != _wheelSeeks) _wheelAccumulator = 0;
        _wheelSeeks = seek;
        _wheelAccumulator += delta;
        while (Math.Abs(_wheelAccumulator) >= NativeMethods.WHEEL_DELTA)
        {
            int up = _wheelAccumulator > 0 ? 1 : -1; // positive = away from the user
            _wheelAccumulator -= up * NativeMethods.WHEEL_DELTA;
            if (seek) Seek(up * _settings.SeekWheelSeconds);
            else SelectRelative(up);
        }
    }

    private void ShowVideoMenu()
    {
        if (!IsActive) return;
        int count = SelectedVideos.Count;
        var menu = BuildFileMenu(count, run => run());
        menu.PlacementTarget = VideoHost;
        menu.IsOpen = true;
    }

    /// <summary>
    /// Delete, Move..., Move to &lt;last&gt;, Copy..., Copy to &lt;last&gt; for <paramref name="count"/> selected
    /// videos. <paramref name="runner"/> runs the chosen action: directly for the video menu, after
    /// selecting the right file for the gallery (1.8.0). Delete: one file without confirmation
    /// ([Delete] ConfirmFromVideoMenu=false), several files confirmed.
    /// </summary>
    private ContextMenu BuildFileMenu(int count, Action<Action> runner)
    {
        bool canAct = !_fileOpBusy && count > 0;
        string files = count > 1 ? $" {count} files" : ""; // "Delete 3 files" with several selected (1.7.0)
        var menu = new ContextMenu { Placement = PlacementMode.MousePoint };
        menu.Items.Add(CreateMenuItem("Delete" + files, "Del / R", canAct, () => runner(() => DeleteSelected(fromVideoMenu: true))));
        menu.Items.Add(CreateMenuItem("Move" + files + "...", "W", canAct, () => runner(() => MoveSelected(useLastDestination: false))));
        AddLastDestinationItem(menu, "Move" + files + " to ", _settings.LastMoveDestination, "Shift+W", canAct,
            () => runner(() => MoveSelected(useLastDestination: true)));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Copy" + files + "...", "C", canAct, () => runner(() => CopySelected(useLastDestination: false))));
        AddLastDestinationItem(menu, "Copy" + files + " to ", _settings.LastCopyDestination, "Shift+C", canAct,
            () => runner(() => CopySelected(useLastDestination: true)));
        return menu;
    }

    /// <summary>"Move to / Copy to &lt;folder&gt;", only when a previous destination exists (full path in the tooltip).</summary>
    private void AddLastDestinationItem(ContextMenu menu, string prefix, string? last, string gesture, bool enabled, Action action)
    {
        if (string.IsNullOrWhiteSpace(last)) return;
        string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(last));
        if (string.IsNullOrEmpty(name)) name = last;
        var item = CreateMenuItem(prefix + name, gesture, enabled, action);
        item.ToolTip = last;
        menu.Items.Add(item);
    }

    private MenuItem CreateMenuItem(string text, string gesture, bool enabled, Action action)
    {
        // TextBlock header: underscores in folder names must not become access keys.
        var item = new MenuItem { Header = new TextBlock { Text = text }, InputGestureText = gesture, IsEnabled = enabled };
        // Run after the menu has closed, so dialogs (confirmation, folder picker) open on a clean state.
        item.Click += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Background, action);
        return item;
    }
}
