using System.Windows;
using ValorantTuner.Services;
namespace ValorantTuner;
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            Logger.Error("Unhandled UI exception", e.Exception);
            MessageBox.Show(e.Exception.Message, "Valorant Tuner", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }
}
