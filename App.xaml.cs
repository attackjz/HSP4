using System.Configuration;
using System.Data;
using System.Windows;

namespace HijackSystem;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

        MainWindow mainWindow = new MainWindow();
        mainWindow.InitWindowWithArgs(args);
        mainWindow.Show();

        // Shutdown the application
        //Application.Current.Shutdown();
    }
}

