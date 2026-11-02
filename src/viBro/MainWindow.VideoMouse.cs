using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;
using ViBro.Util;

namespace ViBro;

// Mouse on the video. The video is mpv's native window (mouse input disabled in mpv), so WPF gets
// no mouse events from it: the messages are read in OnThreadPreprocessMessage and handled here.
//  - single click: Play/Pause at once; double click: fullscreen on/off and the pause toggle of
//    its first click is undone (the video only stops for an instant)
//  - the click that brings viBro back to the foreground only activates the window
//  - right click: menu with Delete, Move..., Move to <last folder>
//  - wheel in fullscreen (anywhere in the window): down = next video, up = previous
public partial class MainWindow
{
    private uint _lastClickTime;
    private int _lastClickX, _lastClickY;
    private bool _hasLastClick;
    private bool _lastClickToggledPause;
    private uint _activatedTick; // GetTickCount time of the last window activation
    private int _wheelAccumulator;

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
                break;

            // mpv's window may or may not have CS_DBLCLKS: the second click arrives as WM_LBUTTONDOWN
            // or WM_LBUTTONDBLCLK. Both are treated as a click and double clicks are detected here,
            // with the system double-click time and distance.
            case NativeMethods.WM_LBUTTONDOWN:
            case NativeMethods.WM_LBUTTONDBLCLK:
                OnVideoClick(unchecked((uint)msg.time), msg.pt_x, msg.pt_y);
                break;

            case NativeMethods.WM_RBUTTONUP:
                ShowVideoMenu();
                break;
        }
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
    private bool IsPointOverVideo(int screenX, int screenY)
    {
        if (!VideoHost.IsVisible) return false;
        Point p = VideoHost.PointFromScreen(new Point(screenX, screenY));
        return p.X >= 0 && p.Y >= 0 && p.X < VideoHost.ActualWidth && p.Y < VideoHost.ActualHeight;
    }

    /// <summary>Main window or one of its children (the video, mpv's window).</summary>
    private bool IsOwnWindowMessage(IntPtr hwnd)
    {
        IntPtr main = new WindowInteropHelper(this).Handle;
        return main != IntPtr.Zero && (hwnd == main || NativeMethods.IsChild(main, hwnd));
    }

    /// <summary>One wheel notch (120) = one video; smaller steps (touchpads) are accumulated.</summary>
    private bool OnFullscreenWheel(int delta)
    {
        if (!IsActive || delta == 0) return false;
        if (Math.Sign(delta) != Math.Sign(_wheelAccumulator)) _wheelAccumulator = 0; // direction changed
        _wheelAccumulator += delta;
        while (Math.Abs(_wheelAccumulator) >= NativeMethods.WHEEL_DELTA)
        {
            int step = _wheelAccumulator > 0 ? -1 : +1; // away from the user (up) = previous
            _wheelAccumulator += step * NativeMethods.WHEEL_DELTA;
            SelectRelative(step);
        }
        return true;
    }

    private void ShowVideoMenu()
    {
        if (!IsActive) return;
        bool canAct = !_fileOpBusy && FileList.SelectedItem is VideoItem;

        var menu = new ContextMenu
        {
            PlacementTarget = VideoHost,
            Placement = PlacementMode.MousePoint,
        };
        menu.Items.Add(CreateMenuItem("Delete", "Del / R", canAct, DeleteSelected));
        menu.Items.Add(CreateMenuItem("Move...", "W", canAct, () => MoveSelected(useLastDestination: false)));

        string? last = _settings.LastMoveDestination;
        if (!string.IsNullOrWhiteSpace(last))
        {
            string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(last));
            if (string.IsNullOrEmpty(name)) name = last;
            var item = CreateMenuItem("Move to " + name, "Shift+W", canAct, () => MoveSelected(useLastDestination: true));
            item.ToolTip = last;
            menu.Items.Add(item);
        }
        menu.IsOpen = true;
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
