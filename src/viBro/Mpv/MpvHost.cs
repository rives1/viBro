using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using ViBro.Util;

namespace ViBro.Mpv;

/// <summary>
/// Native child window that libmpv renders into (mpv option "wid").
/// WPF content cannot be drawn on top of it (airspace), which is acceptable:
/// all controls live outside the video area.
/// </summary>
internal sealed class MpvHost : HwndHost
{
    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_CLIPSIBLINGS = 0x04000000;
    private const int WS_CLIPCHILDREN = 0x02000000;
    private const int SS_BLACKRECT = 0x00000004;

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        IntPtr hwnd = NativeMethods.CreateWindowEx(
            0, "STATIC", string.Empty,
            WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN | SS_BLACKRECT,
            0, 0, 1, 1,
            hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (hwnd == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return new HandleRef(this, hwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
        => NativeMethods.DestroyWindow(hwnd.Handle);
}
