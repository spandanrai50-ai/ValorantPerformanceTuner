using System.Windows;

namespace ValorantTuner;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "An unexpected error occurred. No system settings were changed.\n\n" + args.Exception.Message,
                "Valorant Tuner",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}

