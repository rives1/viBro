using System;
using System.Runtime.InteropServices;

namespace ViBro.Mpv;

// Minimal P/Invoke surface for libmpv client API v2 (client.h).
// Enum values and struct layouts mirror client.h; do not reorder.

internal enum MpvFormat
{
    None = 0,
    String = 1,
    OsdString = 2,
    Flag = 3,
    Int64 = 4,
    Double = 5,
    Node = 6,
    NodeArray = 7,
    NodeMap = 8,
    ByteArray = 9,
}

internal enum MpvEventId
{
    None = 0,
    Shutdown = 1,
    LogMessage = 2,
    GetPropertyReply = 3,
    SetPropertyReply = 4,
    CommandReply = 5,
    StartFile = 6,
    EndFile = 7,
    FileLoaded = 8,
    ClientMessage = 16,
    VideoReconfig = 17,
    AudioReconfig = 18,
    Seek = 20,
    PlaybackRestart = 21,
    PropertyChange = 22,
    QueueOverflow = 24,
    Hook = 25,
}

internal enum MpvEndFileReason
{
    Eof = 0,
    Stop = 2,
    Quit = 3,
    Error = 4,
    Redirect = 5,
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEvent
{
    public MpvEventId EventId;
    public int Error;
    public ulong ReplyUserdata;
    public IntPtr Data;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEventProperty
{
    public IntPtr Name;
    public MpvFormat Format;
    public IntPtr Data;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEventStartFile
{
    public long PlaylistEntryId;
}

// mpv_node: 8-byte union followed by the format tag.
[StructLayout(LayoutKind.Sequential)]
internal struct MpvNode
{
    public IntPtr U;
    public MpvFormat Format;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvNodeList
{
    public int Num;
    public IntPtr Values;
    public IntPtr Keys;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEventEndFile
{
    public int Reason;
    public int Error;
    public long PlaylistEntryId;
    public long PlaylistInsertId;
    public int PlaylistInsertNumEntries;
}

internal static class MpvNative
{
    private const string Lib = "libmpv-2.dll";

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong mpv_client_api_version();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_create();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_initialize(IntPtr ctx);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_terminate_destroy(IntPtr ctx);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_option(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name, MpvFormat format, ref long data);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_option_string(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string data);

    // args: NULL-terminated array of UTF-8 strings. mpv copies the arguments before returning.
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_command_async(IntPtr ctx, ulong replyUserdata, IntPtr[] args);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_get_property(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name, MpvFormat format, out double data);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_get_property_string(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_observe_property(IntPtr ctx, ulong replyUserdata,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name, MpvFormat format);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_wait_event(IntPtr ctx, double timeout);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_wakeup(IntPtr ctx);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_free(IntPtr data);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_error_string(int error);

    public static string ErrorString(int error)
        => Marshal.PtrToStringUTF8(mpv_error_string(error)) ?? $"mpv error {error}";
}
