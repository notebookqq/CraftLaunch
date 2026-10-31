using System.Windows.Controls;
using System.Windows.Input;
using CraftLaunch.ViewModels;

namespace CraftLaunch.Views;

public partial class VersionDownloadPage : UserControl
{
    public VersionDownloadPage()
    {
        InitializeComponent();
    }

    private void OnVersionDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is VersionDownloadViewModel { SelectedRemoteVersion: not null } vm && vm.DownloadCommand.CanExecute(null))
            vm.DownloadCommand.Execute(null);
    }
}
