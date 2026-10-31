using System.Windows.Controls;

namespace CraftLaunch.Views;

public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
        LogBox.TextChanged += (_, _) => LogBox.ScrollToEnd();
    }
}
