using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace ViBro.Util;

internal static class NativeMethods
{
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONDBLCLK = 0x0203;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;
    public const int MK_RBUTTON = 0x0002;
    public const int WM_MOUSEWHEEL = 0x020A;
    public const int WHEEL_DELTA = 120;

    private const int SM_CXDOUBLECLK = 36;
    private const int SM_CYDOUBLECLK = 37;

    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style,
        int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsChild(IntPtr parent, IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int virtualKey);

    [DllImport("user32.dll")]
    public static extern uint GetDoubleClickTime();

    [DllImport("user32.dll")]
    public static extern IntPtr SetCapture(IntPtr hwnd);

    /// <summary>False while a Win32 modal (MessageBox, folder picker) has disabled the window.</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowEnabled(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReleaseCapture();

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO { public int cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

    /// <summary>Work area (device pixels) of the monitor containing the point, or null.</summary>
    public static (int Left, int Top, int Right, int Bottom)? WorkAreaAt(int x, int y)
    {
        IntPtr m = MonitorFromPoint(new POINT { X = x, Y = y }, 2 /* MONITOR_DEFAULTTONEAREST */);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (m == IntPtr.Zero || !GetMonitorInfo(m, ref info)) return null;
        return (info.rcWork.Left, info.rcWork.Top, info.rcWork.Right, info.rcWork.Bottom);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, System.Text.StringBuilder name, int maxCount);

    /// <summary>Window class name, for diagnostics.</summary>
    public static string ClassName(IntPtr hwnd)
    {
        var sb = new System.Text.StringBuilder(256);
        return GetClassName(hwnd, sb, sb.Capacity) > 0 ? sb.ToString() : "?";
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    /// <summary>Size of the rectangle within which two clicks count as a double click (system setting).</summary>
    public static (int Width, int Height) DoubleClickSize() => (GetSystemMetrics(SM_CXDOUBLECLK), GetSystemMetrics(SM_CYDOUBLECLK));

    /// <summary>Signed wheel rotation of a WM_MOUSEWHEEL (high word of wParam; positive = away from the user).</summary>
    public static int WheelDelta(IntPtr wParam) => (short)((wParam.ToInt64() >> 16) & 0xFFFF);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int StrCmpLogicalW(string a, string b);

    /// <summary>Modifier state read directly from Win32 (valid even when focus is in a native child).</summary>
    public static ModifierKeys CurrentModifiers()
    {
        var m = ModifierKeys.None;
        if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) m |= ModifierKeys.Shift;
        if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) m |= ModifierKeys.Control;
        if ((GetKeyState(VK_MENU) & 0x8000) != 0) m |= ModifierKeys.Alt;
        return m;
    }
}
