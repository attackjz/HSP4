using System.Text.Json;
using System.Windows;

namespace HijackSystem;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        RevertButton.Click += new RoutedEventHandler(RevertButton_Click);

    }

    public void InitWindowWithArgs(string[] args)
    {
        hijackFileManager.InitWithArgs(args);
        RefreshPathInfoView();
    }

    private void RefreshPathInfoView()
    {
        HijackList.Items.Clear();
        foreach (var pathInfo in hijackFileManager.PathInfoList)
        {
            HijackList.Items.Add(pathInfo.depotPath);
        }
    }

    private void RevertButton_Click(object sender, System.EventArgs e)
    {
        List<String> pendingRevertList = HijackList.SelectedItems.Cast<string>().ToList();
        hijackFileManager.RevertWithDepotList(pendingRevertList);
        RefreshPathInfoView();
    }

    HijackFileManager hijackFileManager = new HijackFileManager();
}