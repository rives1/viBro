using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ViBro.Util;

/// <summary>
/// libmpv-2.dll imports vulkan-1.dll statically. GPU drivers install it in System32, but a PC or VM
/// without GPU drivers may lack it, and then libmpv cannot load at all. The official Khronos loader
/// is bundled in the EXE. It is used only when the system has none: once a module named
/// vulkan-1.dll is loaded, Windows binds libmpv's import to it, so the system loader (usually newer)
/// keeps priority. Without a Vulkan driver mpv simply uses Direct3D 11.
/// </summary>
internal static class NativePreload
{
    /// <returns>"system", "bundled" or "missing".</returns>
    public static string EnsureVulkanLoader()
    {
        if (!OperatingSystem.IsWindows()) return "system";

        string system = Path.Combine(Environment.SystemDirectory, "vulkan-1.dll");
        if (File.Exists(system) && NativeLibrary.TryLoad(system, out _)) return "system";

        // Same probing as [DllImport]: includes the single-file extraction folder where libmpv lives.
        if (NativeLibrary.TryLoad("vulkan-1.dll", typeof(NativePreload).Assembly, null, out _))
        {
            ErrorLog.Write("startup", "No system vulkan-1.dll: using the bundled Vulkan loader.");
            return "bundled";
        }
        return "missing";
    }
}
