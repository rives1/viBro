using System;
using System.Threading.Tasks;
using System.Windows;
using ViBro.Util;

namespace ViBro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Unexpected errors: logged to viBro-errors.log and shown, without closing the program
        // when it can continue (UI thread); background task errors are only logged.
        DispatcherUnhandledException += (_, args) =>
        {
            ErrorLog.Write("crash", "Unhandled UI exception", args.Exception);
            MessageBox.Show(
                "An unexpected error occurred:\n\n" + args.Exception.Message +
                $"\n\nDetails were written to {ErrorLog.FilePath}",
                "viBro - unexpected error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ErrorLog.Write("crash", "Unhandled exception (terminating: " + args.IsTerminating + ")", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ErrorLog.Write("task", "Unobserved task exception", args.Exception);
            args.SetObserved();
        };

        // libmpv imports vulkan-1.dll: use the system loader when present, else the bundled one.
        string vulkan = NativePreload.EnsureVulkanLoader();
        if (vulkan == "missing")
            ErrorLog.Write("startup", "vulkan-1.dll not found (system or bundled); the video engine may not start.");

        var window = new MainWindow(StartupOptions.Parse(e.Args));
        MainWindow = window;
        window.Show();
    }
}
