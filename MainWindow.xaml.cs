using System.Windows;
using ValorantTuner.ViewModels;
namespace ValorantTuner;
public partial class MainWindow : Window
{
    public MainWindow() { InitializeComponent(); DataContext = new MainViewModel(); }
}
