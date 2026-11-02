using System.Windows;
using ViBro.Util;

namespace ViBro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "viBro - unexpected error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        var window = new MainWindow(StartupOptions.Parse(e.Args));
        MainWindow = window;
        window.Show();
    }
}
